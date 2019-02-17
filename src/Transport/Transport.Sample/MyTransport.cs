using RabbitMQ.Client;
using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.RabbitMq;
using Rebus.Subscriptions;
using Rebus.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Transport.Sample
{
    public class MyTransport : ITransport, IDisposable, IInitializable, ISubscriptionStorage
    {
        private readonly RabbitMqTransport transport;
        private readonly IList<ConnectionEndpoint> endpoints;
        private readonly string connectionString;
        private readonly string delayExchangeName;
        private readonly Func<IConnectionFactory, IConnectionFactory> customizer;

        private const string DELAY_EXCHANGE_TYPE = "x-delayed-message";

        public MyTransport(IList<ConnectionEndpoint> endpoints, string inputQueueAddress, IRebusLoggerFactory rebusLoggerFactory, int maxMessagesToPrefetch = 50, Func<IConnectionFactory, IConnectionFactory> customizer = null, string delayExchangeName = "Delay.Exchange")
        {
            transport = new RabbitMqTransport(endpoints, inputQueueAddress, rebusLoggerFactory, maxMessagesToPrefetch, customizer);

            this.delayExchangeName = delayExchangeName;
            this.endpoints = endpoints;
            this.customizer = customizer;
        }

        public MyTransport(string connectionString, string inputQueueAddress, IRebusLoggerFactory rebusLoggerFactory, int maxMessagesToPrefetch = 50, Func<IConnectionFactory, IConnectionFactory> customizer = null, string delayExchangeName = "Delay.Exchange")
        {
            transport = new RabbitMqTransport(connectionString, inputQueueAddress, rebusLoggerFactory, maxMessagesToPrefetch, customizer);

            this.delayExchangeName = delayExchangeName;
            this.connectionString = connectionString;
            this.customizer = customizer;
        }

        public string Address => transport.Address;

        public bool IsCentralized => transport.IsCentralized;

        public void CreateQueue(string address)
        {
            transport.CreateQueue(address);
        }

        public void Dispose()
        {
            transport.Dispose();
        }

        public Task<string[]> GetSubscriberAddresses(string topic)
        {
            return transport.GetSubscriberAddresses(topic);
        }

        public void Initialize()
        {
            transport.Initialize();
            CreateDelayExchange();
        }

        public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
        {
            return await transport.Receive(context, cancellationToken);
        }

        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            var connection = CreateConnection();

            try
            {
                using (var model = connection.CreateModel())
                {
                    model.QueueBind(Address, delayExchangeName, topic);
                }
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception, $"Erro bind queue '{Address}' at exchange '{delayExchangeName}' with topic '{topic}'");
            }

            await transport.RegisterSubscriber(topic, subscriberAddress);
        }

        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            // publish to delay exchange
            if (message.Headers != null && message.Headers.ContainsKey(Rebus.Messages.Headers.DeferredUntil))
            {
                var defferValue = message.Headers[Rebus.Messages.Headers.DeferredUntil];
                var rountingKey = message.Headers[Rebus.Messages.Headers.Type];
                var defferDate = DateTime.Parse(defferValue);
                var defferMilleseconds = (defferDate - DateTime.Now).TotalMilliseconds;
                defferMilleseconds = Math.Max(0, Math.Ceiling(defferMilleseconds));

                try
                {
                    var connection = CreateConnection();

                    using (var model = connection.CreateModel())
                    {
                        var props = CreateBasicProperties(model, message.Headers);
                        props.Headers.Add("x-delay", defferMilleseconds.ToString());
                        props.Headers.Remove(Rebus.Messages.Headers.DeferredUntil);

                        var mandatory = message.Headers.ContainsKey(RabbitMqHeaders.Mandatory);

                        model.BasicPublish(
                            exchange: delayExchangeName,
                            routingKey: rountingKey,
                            mandatory: mandatory,
                            basicProperties: props,
                            body: message.Body
                        );
                    }
                }
                catch (Exception exception)
                {
                    throw new RebusApplicationException(exception, $"Excanche declaration for '{delayExchangeName}' failed");
                }
            }
            else
                await transport.Send(destinationAddress, message, context);
        }

        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            await transport.UnregisterSubscriber(topic, subscriberAddress);
        }

        private void CreateDelayExchange()
        {
            var connection = CreateConnection();

            try
            {
                using (var model = connection.CreateModel())
                {
                    model.ExchangeDeclare(delayExchangeName, DELAY_EXCHANGE_TYPE, true,
                        arguments: new Dictionary<string, object>() {
                        {"x-delayed-type", "direct" }
                    });

                    //model.QueueBind(Address, delayExchangeName, Address);
                }
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception, $"Excanche declaration for '{delayExchangeName}' failed");
            }
        }

        private IConnection CreateConnection()
        {
            IConnectionFactory connectionFactory;

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                var uriStrings = connectionString.Split(";,".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                connectionFactory = new ConnectionFactory
                {
                    Uri = new Uri(uriStrings.First()), //Use the first URI in the list for ConnectionFactory to pick the AMQP credentials (if any)
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(30)
                };
            }
            else
            {
                connectionFactory = new ConnectionFactory
                {
                    Uri = endpoints.First().ConnectionUri, //Use the first URI in the list for ConnectionFactory to pick the AMQP credentials, VirtualHost (if any)
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(30)
                };
            }

            if (customizer != null)
            {
                connectionFactory = customizer(connectionFactory);
            }

            return connectionFactory.CreateConnection(); ;
        }

        private IBasicProperties CreateBasicProperties(IModel model, Dictionary<string, string> headers)
        {
            var props = model.CreateBasicProperties();

            if (headers.TryGetValue(RabbitMqHeaders.MessageId, out var messageId))
            {
                props.MessageId = messageId;
            }

            if (headers.TryGetValue(RabbitMqHeaders.AppId, out var appId))
            {
                props.AppId = appId;
            }

            if (headers.TryGetValue(RabbitMqHeaders.CorrelationId, out var correlationId))
            {
                props.CorrelationId = correlationId;
            }

            if (headers.TryGetValue(RabbitMqHeaders.UserId, out var userId))
            {
                props.UserId = userId;
            }

            if (headers.TryGetValue(RabbitMqHeaders.ContentType, out var contentType))
            {
                props.ContentType = contentType;
            }

            if (headers.TryGetValue(RabbitMqHeaders.ContentEncoding, out var contentEncoding))
            {
                props.ContentEncoding = contentEncoding;
            }

            if (headers.TryGetValue(RabbitMqHeaders.DeliveryMode, out var deliveryModeVal))
            {
                if (byte.TryParse(deliveryModeVal, out var deliveryMode) && deliveryMode > 0 && deliveryMode <= 2)
                {
                    props.DeliveryMode = deliveryMode;
                }
            }

            if (headers.TryGetValue(RabbitMqHeaders.Expiration, out var expiration))
            {
                if (TimeSpan.TryParse(expiration, out var timeToBeDelivered))
                {
                    props.Expiration = timeToBeDelivered.TotalMilliseconds.ToString("0");
                }
            }

            if (headers.TryGetValue(RabbitMqHeaders.Priority, out var priorityVal))
            {
                if (byte.TryParse(priorityVal, out var priority))
                {
                    props.Priority = priority;
                }
            }

            if (headers.TryGetValue(RabbitMqHeaders.Timestamp, out var timestampVal))
            {
                if (DateTimeOffset.TryParse(timestampVal, out var timestamp))
                {
                    // Unix epoch
                    var unixTime = (long)(timestamp.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
                    props.Timestamp = new AmqpTimestamp(unixTime);
                }
            }

            if (headers.TryGetValue(RabbitMqHeaders.Type, out var type))
            {
                props.Type = type;
            }

            // Alternative way of setting RabbitMqHeaders.DeliveryMode
            if (!headers.ContainsKey(RabbitMqHeaders.DeliveryMode))
            {
                var express = headers.ContainsKey(Rebus.Messages.Headers.Express);
                props.Persistent = !express;
            }

            // must be last because the other functions on the headers might change them
            props.Headers = headers
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value != null ? (object)Encoding.UTF8.GetBytes(kvp.Value) : null);

            return props;
        }

        private string GetRountingKey(string destinationAddress)
        {
            var tokens = destinationAddress.Split('@');

            if (tokens.Length > 1)
            {
                return string.Join("@", tokens.Take(tokens.Length - 1));
            }
            else
            {
                return destinationAddress;
            }

        }
    }
}
