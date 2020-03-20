using System;
using System.Collections.Generic;

namespace FluentBus.IntegrationTests
{
    public interface IPublisherFactory
    {
        IPublisher Get(string name);
    }

    public class PublisherFactory :
        Dictionary<string, Func<IPublisher>>, IPublisherFactory
    {
        public IPublisher Get(string name)
        {
            if (this.ContainsKey(name))
                return this[name]();
            if (this.ContainsKey(string.Empty))
                return this[string.Empty]();
            throw new NotImplementedException();
        }
    }
}