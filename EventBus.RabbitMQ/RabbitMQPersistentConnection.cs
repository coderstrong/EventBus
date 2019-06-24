using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.IO;
using System.Net.Sockets;

namespace EventBus.RabbitMQ
{
    public class RabbitMQPersistentConnection : IRabbitMQPersistentConnection
    {
        private static readonly object LockObj = new object();
        private bool _disposed;
        private RabbitMQOptions _options;
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private readonly ILogger<RabbitMQPersistentConnection> _logger;

        public RabbitMQPersistentConnection(ILogger<RabbitMQPersistentConnection> logger, IOptions<RabbitMQOptions> optionsAccessor)
        {
            _logger = logger;

            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            _options = optionsAccessor.Value;

            _connectionFactory = new ConnectionFactory()
            {
                HostName = _options.HostName,
                Port = _options.Port
            };

            if (!string.IsNullOrWhiteSpace(_options.UserName))
            {
                _connectionFactory.UserName = _options.UserName;
            }

            if (!string.IsNullOrWhiteSpace(_options.Password))
            {
                _connectionFactory.Password = _options.Password;
            }

            this.PersistentConnect();
        }

        public bool IsConnected
        {
            get
            {
                return _connection != null && _connection.IsOpen && !_disposed;
            }
        }

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            return _connection.CreateModel();
        }

        public bool PersistentConnect()
        {
            _logger.LogInformation("RabbitMQ Client is trying to connect");

            lock (LockObj)
            {
                var policy = RetryPolicy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(_options.RetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        _logger.LogError(ex, "TryConnect RabbitMQ");
                    }
                );

                policy.Execute(() =>
                {
                    _connection = _connectionFactory.CreateConnection();
                });

                if (IsConnected)
                {
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;

                    _logger.LogInformation($"RabbitMQ persistent connection acquired a connection {_connection.Endpoint.HostName} and is subscribed to failure events");

                    return true;
                }
                else
                {
                    _logger.LogError("FATAL ERROR: RabbitMQ connections could not be created and opened");

                    return false;
                }
            }
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs reason)
        {
            if (_disposed) return;

            _logger.LogError("RabbitMQ connection is blocked because {@reason}. Trying to re-connect...", reason);

            this.PersistentConnect();
        }

        void OnCallbackException(object sender, CallbackExceptionEventArgs reason)
        {
            if (_disposed) return;

            _logger.LogError("RabbitMQ connection is throw because {@reason}. Trying to re-connect...", reason);

            this.PersistentConnect();
        }

        void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;

            _logger.LogError("RabbitMQ connection is on shutdown because {@reason}. Trying to re-connect...", reason);

            this.PersistentConnect();
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                if (_connection != null)
                    _connection.Dispose();
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Dispose Connect RabbitMQ");
            }
        }
    }
}
