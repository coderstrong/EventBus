using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.RabbitMQ
{
    public class EventBusRabbitMQ : IEventBus
    {
        private IServiceScopeFactory _serviceScope;
        private IRabbitMQPersistentConnection _connection;
        private ILogger<EventBusRabbitMQ> _logger;
        private RabbitMQOptions _options;
        private IEventBusManager _manager;
        private IModel _channel;
        private string _token;
        public EventBusRabbitMQ(IServiceScopeFactory serviceScope, IOptions<RabbitMQOptions> optionsAccessor,
            IRabbitMQPersistentConnection persistentConnection, IEventBusManager manager, ILogger<EventBusRabbitMQ> logger, IHttpContextAccessor httpContextAccessor)
        {
            _serviceScope = serviceScope;
            _connection = persistentConnection;
            _manager = manager;
            _logger = logger;

            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            if (httpContextAccessor.HttpContext != null)
            {
                var bearerToken = httpContextAccessor.HttpContext.Request
                                  .Headers["Authorization"]
                                  .FirstOrDefault(h => h.StartsWith("bearer ", StringComparison.InvariantCultureIgnoreCase));

                // Add authorization if found
                if (bearerToken != null)
                {
                    _token = bearerToken;
                    _logger.LogInformation("Found Authorization : {0}", bearerToken);
                }
            }

            _options = optionsAccessor.Value;

            if (_options.IsConsummer)
            {
                _channel = CreateConsumerChannel();
            }
        }

        public void Publish<TIntegrationEvent>(TIntegrationEvent eventData) where TIntegrationEvent : IntegrationEvent
        {
            var eventType = eventData.GetType();
            var _queueName = _options.QueueName;
            var _exchange = _options.ExchangeName;
            var _exchangeType = _options.ExchangeType;
            var _routingKey = eventType.Name;
            var _retryCount = _options.RetryCount;

            //Settings Queue
            var _druable = _options.Druable;
            var _exclusive = _options.Exclusive;
            var _autodelete = _options.AutoDelete;
            var _args = _options.Args;

            var _settings = eventType.GetCustomAttributes(false).FirstOrDefault(x => x is RabbitMQSettingsAttribute) as RabbitMQSettingsAttribute;

            // Reset Config if has Attribute
            if (_settings != null)
            {
                if (!string.IsNullOrEmpty(_settings.ExchangeName))
                {
                    _exchange = _settings.ExchangeName;
                }

                if (!string.IsNullOrEmpty(_settings.ExchangeType))
                {
                    _exchangeType = _settings.ExchangeType;
                }

                if (!string.IsNullOrEmpty(_settings.QueueName))
                {
                    _queueName = _settings.QueueName;
                }

                if (!string.IsNullOrEmpty(_settings.RootingKey))
                {
                    _routingKey = _settings.RootingKey;
                }

                _druable = _settings.Druable;
                _exclusive = _settings.Exclusive;
                _autodelete = _settings.AutoDelete;
                _args = _settings.Args;
            }

            using (var channel = _connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: _exchange, type: _exchangeType);

                channel.QueueDeclare(_queueName, _druable, _exclusive, _autodelete, _args);

                channel.QueueBind(queue: _queueName, exchange: _exchange, routingKey: _routingKey);

                eventData.JwtToken = _token;

                var message = JsonConvert.SerializeObject(eventData);
                var body = Encoding.UTF8.GetBytes(message);

                var policy = RetryPolicy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogError(ex, "Publish");
                });

                policy.Execute(() =>
                {
                    var properties = channel.CreateBasicProperties();

                    properties.DeliveryMode = eventData.DeliveryMode; //1 none-persistent, 2 persistent
                    properties.ContentEncoding = "UTF8";
                    properties.ContentType = "application/json";

                    channel.BasicPublish(exchange: _exchange,
                                     routingKey: _routingKey,
                                     mandatory: _settings.Mandatory,
                                     basicProperties: properties,
                                     body: body);
                });
            }
        }

        public Task PublishAsync<TIntegrationEvent>(TIntegrationEvent eventData) where TIntegrationEvent : IntegrationEvent
        {
            return Task.Run(() => Publish<TIntegrationEvent>(eventData));
        }

        public void Subscribe<TIntegrationEvent, TIIntegrationEventHandler>()
            where TIntegrationEvent : IntegrationEvent
            where TIIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>
        {
            Subscribe(typeof(TIntegrationEvent), typeof(TIIntegrationEventHandler));
        }

        public void Subscribe(Type eventType, Type handler)
        {
            if (!_manager.HasSubscribeForEvent(eventType))
            {
                using (var channel = _connection.CreateModel())
                {
                    var _queueName = _options.QueueName;
                    var _exchange = _options.ExchangeName;
                    var _routingKey = eventType.Name;

                    //Settings Queue
                    var _druable = _options.Druable;
                    var _exclusive = _options.Exclusive;
                    var _autodelete = _options.AutoDelete;
                    var _args = _options.Args;

                    var _settings = eventType.GetCustomAttributes(false).FirstOrDefault(x => x is RabbitMQSettingsAttribute) as RabbitMQSettingsAttribute;

                    // Reset Config if has Attribute
                    if (_settings != null)
                    {
                        if (!string.IsNullOrEmpty(_settings.ExchangeName))
                        {
                            _exchange = _settings.ExchangeName;
                        }

                        if (!string.IsNullOrEmpty(_settings.QueueName))
                        {
                            _queueName = _settings.QueueName;
                        }

                        if (!string.IsNullOrEmpty(_settings.RootingKey))
                        {
                            _routingKey = _settings.RootingKey;
                        }

                        //Settings Queue
                        _druable = _settings.Druable;
                        _exclusive = _settings.Exclusive;
                        _autodelete = _settings.AutoDelete;
                        _args = _settings.Args;
                    }

                    channel.QueueDeclare(_queueName, _druable, _exclusive, _autodelete, _args);

                    channel.QueueBind(queue: _queueName, exchange: _exchange, routingKey: _routingKey);
                }
            }
            _manager.AddSubscribe(eventType, handler);
        }

        public void UnSubscribe<TIntegrationEvent, TIIntegrationEventHandler>()
            where TIntegrationEvent : IntegrationEvent
            where TIIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>
        {
            _manager.RemoveSubscribe<TIntegrationEvent, TIIntegrationEventHandler>();
        }

        public void UnSubscribeAll<TIntegrationEvent>() where TIntegrationEvent : IntegrationEvent
        {
            List<Type> handlerTypes = _manager.GetHandlersForEvent(typeof(TIntegrationEvent)).ToList();
            foreach (var handlerType in handlerTypes)
            {
                _manager.RemoveSubscribe(typeof(TIntegrationEvent), handlerType);
            }
        }

        public IModel CreateConsumerChannel()
        {
            var _queueName = _options.QueueName;
            var _exchange = _options.ExchangeName;
            var _exchangeType = _options.ExchangeType;

            var channel = _connection.CreateModel();

            channel.ExchangeDeclare(exchange: _exchange, type: _exchangeType);

            channel.QueueDeclare(_queueName, _options.Druable, _options.Exclusive, _options.AutoDelete, _options.Args);

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += async (model, ea) =>
            {
                var eventName = ea.RoutingKey;
                var message = Encoding.UTF8.GetString(ea.Body);

                var eventData = JsonConvert.DeserializeObject<IntegrationEvent>(message);

                bool? result = (await HandleEvent(eventName, message)) as bool?;

                if (eventData.DeliveryMode == 2)
                {
                    if (result != null)
                    {
                        if (!result.Value)
                        {
                            channel.BasicReject(ea.DeliveryTag, true);
                        }
                        else
                        {
                            channel.BasicAck(ea.DeliveryTag, multiple: false);
                        }
                    }
                }
                else
                {
                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
            };

            channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

            channel.CallbackException += (sender, ea) =>
            {
                _channel.Dispose();
                _channel = CreateConsumerChannel();
            };

            return channel;
        }

        private async Task<object> HandleEvent(string eventName, string message)
        {
            var eventType = _manager.GetEventTypeByName(eventName);

            if (eventType != null)
            {
                if (_manager.HasSubscribeForEvent(eventType))
                {
                    var handlerTypes = _manager.GetHandlersForEvent(eventType);

                    if (handlerTypes.Count() > 0)
                    {
                        using (var scope = _serviceScope.CreateScope())
                        {
                            foreach (var handlerType in handlerTypes)
                            {
                                if (typeof(IIntegrationEventHandler).IsAssignableFrom(handlerType))
                                {
                                    var handler = scope.ServiceProvider.GetRequiredService(handlerType);
                                    if (handler == null) continue;
                                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                                    return await concreteType.GetMethod("Handle").InvokeAsync(handler, new object[] { JsonConvert.DeserializeObject(message, eventType) });
                                }
                                else
                                {
                                    var handler = scope.ServiceProvider.GetRequiredService(handlerType) as IDynamicEventHandler;
                                    if (handler == null) continue;
                                    return await handler.Handle(JsonConvert.DeserializeObject(message));
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
