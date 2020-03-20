using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace FluentBus.Core.KafkaSubscription
{
    public class KafkaConsumerClient : IConsumerClient
    {
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private readonly string _groupId;
        private readonly KafkaOptions _kafkaOptions;
        private IConsumer<string, byte[]> _consumerClient;

        public KafkaConsumerClient(string groupId, IOptions<KafkaOptions> options)
        {
            _groupId = groupId;
            _kafkaOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public event EventHandler<TransportMessage> OnMessageReceived;

        //public event EventHandler<LogMessageEventArgs> OnLog;

        public BrokerAddress BrokerAddress => new BrokerAddress("Kafka", _kafkaOptions.Servers);

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

            Connect();

            _consumerClient.Subscribe(topics);
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Connect();

            while (true)
            {
                var consumerResult = _consumerClient.Consume(cancellationToken);

                if (consumerResult.IsPartitionEOF || consumerResult.Value == null)
                    continue;

                var headers = new Dictionary<string, string>(consumerResult.Headers.Count);
                foreach (var header in consumerResult.Headers)
                {
                    var val = header.GetValueBytes();
                    headers.Add(header.Key, val != null ? Encoding.UTF8.GetString(val) : null);
                }
                //headers.Add(Messages.Headers.Group, _groupId);

                headers.Add("cap-kafka-key", consumerResult.Key);

                if (_kafkaOptions.CustomHeaders != null)
                {
                    var customHeaders = _kafkaOptions.CustomHeaders(consumerResult);
                    foreach (var customHeader in customHeaders)
                    {
                        headers.Add(customHeader.Key, customHeader.Value);
                    }
                }

                var message = new TransportMessage(headers, consumerResult.Value);

                OnMessageReceived?.Invoke(consumerResult, message);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public void Commit(object sender)
        {
            _consumerClient.Commit((ConsumeResult<string, byte[]>)sender);
        }

        public void Reject(object sender)
        {
            _consumerClient.Assign(_consumerClient.Assignment);
        }

        public void Dispose()
        {
            _consumerClient?.Dispose();
        }

        public void Connect()
        {
            if (_consumerClient != null)
            {
                return;
            }

            _connectionLock.Wait();

            try
            {
                if (_consumerClient == null)
                {
                    _kafkaOptions.MainConfig["group.id"] = _groupId;
                    _kafkaOptions.MainConfig["auto.offset.reset"] = "earliest";
                    var config = _kafkaOptions.AsKafkaConfig();

                    _consumerClient = new ConsumerBuilder<string, byte[]>(config)
                        .SetErrorHandler(ConsumerClient_OnConsumeError)
                        .Build();
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private void ConsumerClient_OnConsumeError(IConsumer<string, byte[]> consumer, Error e)
        {
            //var logArgs = new LogMessageEventArgs
            //{
            //    LogType = MqLogType.ServerConnError,
            //    Reason = $"An error occurred during connect kafka --> {e.Reason}"
            //};
            //OnLog?.Invoke(null, logArgs);
        }
    }


    /// <summary>
    /// Provides programmatic configuration for the CAP kafka project.
    /// </summary>
    public class KafkaOptions
    {
        /// <summary>
        /// librdkafka configuration parameters (refer to https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md).
        /// <para>
        /// Topic configuration parameters are specified via the "default.topic.config" sub-dictionary config parameter.
        /// </para>
        /// </summary>
        public readonly ConcurrentDictionary<string, string> MainConfig;

        private IEnumerable<KeyValuePair<string, string>> _kafkaConfig;


        public KafkaOptions()
        {
            MainConfig = new ConcurrentDictionary<string, string>();
        }

        /// <summary>
        /// Producer connection pool size, default is 10
        /// </summary>
        public int ConnectionPoolSize { get; set; } = 10;

        /// <summary>
        /// The `bootstrap.servers` item config of <see cref="MainConfig" />.
        /// <para>
        /// Initial list of brokers as a CSV list of broker host or host:port.
        /// </para>
        /// </summary>
        public string Servers { get; set; }

        /// <summary>
        /// If you need to get offset and partition and so on.., you can use this function to write additional header into <see cref="CapHeader"/>
        /// </summary>
        public Func<ConsumeResult<string, byte[]>, List<KeyValuePair<string, string>>> CustomHeaders { get; set; }

        internal IEnumerable<KeyValuePair<string, string>> AsKafkaConfig()
        {
            if (_kafkaConfig == null)
            {
                if (string.IsNullOrWhiteSpace(Servers))
                {
                    throw new ArgumentNullException(nameof(Servers));
                }

                MainConfig["bootstrap.servers"] = Servers;
                MainConfig["queue.buffering.max.ms"] = "10";
                MainConfig["enable.auto.commit"] = "false";
                MainConfig["log.connection.close"] = "false";
                MainConfig["request.timeout.ms"] = "3000";
                MainConfig["message.timeout.ms"] = "5000";

                _kafkaConfig = MainConfig.AsEnumerable();
            }

            return _kafkaConfig;
        }
    }
}
