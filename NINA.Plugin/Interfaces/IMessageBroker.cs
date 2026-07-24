using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugin.Interfaces {
    public interface IMessageBroker {
        Task Publish(IMessage message);
        Task Publish(IMessage message, CancellationToken token);
        void Subscribe(string topic, ISubscriber subscriber);
        void Unsubscribe(string topic, ISubscriber subscriber);
    }
}
