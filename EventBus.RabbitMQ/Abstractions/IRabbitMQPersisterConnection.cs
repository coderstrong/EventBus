using RabbitMQ.Client;
using System;

namespace EventBus.RabbitMQ
{
    public interface IRabbitMQPersistentConnection
        : IDisposable
    {
        bool IsConnected { get; }

        bool PersistentConnect();

        IModel CreateModel();
    }
}
