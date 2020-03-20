using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Options;

namespace FluentBus.Core
{

    /// <inheritdoc />
    /// <summary>
    /// Message queue consumer client
    /// </summary>
    public interface IConsumerClient : IDisposable
    {
        BrokerAddress BrokerAddress { get; }

        /// <summary>
        /// Subscribe to a set of topics to the message queue
        /// </summary>
        /// <param name="topics"></param>
        void Subscribe(IEnumerable<string> topics);

        /// <summary>
        /// Start listening
        /// </summary>
        void Listening(TimeSpan timeout, CancellationToken cancellationToken);

        /// <summary>
        /// Manual submit message offset when the message consumption is complete
        /// </summary>
        void Commit(object sender);

        /// <summary>
        /// Reject message and resumption
        /// </summary>
        void Reject(object sender);

        event EventHandler<TransportMessage> OnMessageReceived;
    }
}
