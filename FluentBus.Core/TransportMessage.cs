using System;
using System.Collections.Generic;

namespace FluentBus.Core
{
    /// <summary>
    /// Message content field
    /// </summary>
    public class TransportMessage
    {
        public TransportMessage(IDictionary<string, string> headers,  byte[] body)
        {
            Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            Body = body;
        }

        /// <summary>
        /// Gets the headers of this message
        /// </summary>
        public IDictionary<string, string> Headers { get; }

        /// <summary>
        /// Gets the body object of this message
        /// </summary>
        public byte[] Body { get; }
    }
}
