using RabbitMQ.Client;
using Rebus.Config;
using Rebus.Timeouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Timeout.Sample
{
    public class DelayedMessageExchangeTimeout : ITimeoutManager
    {
        private readonly IConnection connection;
        private readonly string exchangeName;
        private readonly string queue;
        private readonly ISet<string> bindedRountingKeys;

        private const string DELAY_EXCHANGE_TYPE = "x-delayed-message";

        private static object routingKeyBinder = new object();

        public DelayedMessageExchangeTimeout(string endpoint, string queueToBind, string exchangeName)
        {
            this.exchangeName = exchangeName;
            this.queue = queueToBind;

            bindedRountingKeys = new HashSet<string>();

            var connectionFactory = new ConnectionFactory()
            {
                Uri = new Uri(endpoint),
                NetworkRecoveryInterval = TimeSpan.FromSeconds(30)
            };

            connection = connectionFactory.CreateConnection();

            CreateDelayExchange();
        }

        public Task Defer(DateTimeOffset approximateDueTime, Dictionary<string, string> headers, byte[] body)
        {
            var topic = headers[Rebus.Messages.Headers.Type];

            EnsureQueueBinded(topic);

            var diffDatetTime = approximateDueTime.UtcDateTime - DateTime.UtcNow;
            var millisecondsDelay = Math.Ceiling(diffDatetTime.TotalMilliseconds);

            if (millisecondsDelay <= 0)
            {
                throw new Exception($"It's not possible defer message. Due time '{approximateDueTime}' is out of date");
            }

            using (var model = connection.CreateModel())
            {
                var props = model.CreateBasicProperties();
                props.Headers = new Dictionary<string, object>();

                foreach (var header in headers)
                {
                    props.Headers.Add(header.Key, header.Value);
                }

                props.Headers.Add("x-delay", millisecondsDelay.ToString());

                model.BasicPublish(exchangeName, topic, props, body);
            }

            return Task.CompletedTask;
        }

        public Task<DueMessagesResult> GetDueMessages()
        {
            return Task.FromResult(new DueMessagesResult(new List<DueMessage>()));
        }

        private void CreateDelayExchange()
        {
            using (var model = connection.CreateModel())
            {
                model.ExchangeDeclare(exchangeName, DELAY_EXCHANGE_TYPE, true,
                    arguments: new Dictionary<string, object>() {
                        {"x-delayed-type", "direct" }
                });
            }
        }

        private void EnsureQueueBinded(string rountingKey)
        {
            if (!bindedRountingKeys.Contains(rountingKey))
            {
                lock (routingKeyBinder)
                {
                    if (!bindedRountingKeys.Contains(rountingKey))
                    {
                        using (var model = connection.CreateModel())
                        {
                            model.QueueBind(queue, exchangeName, rountingKey);
                        }

                        bindedRountingKeys.Add(rountingKey);
                    }
                }
            }
        }
    }

    public static class DelayedMessageExchangeConfigurationExtension
    {
        public static void UseRabbitMqDelayed(this StandardConfigurer<ITimeoutManager> configurer, string endpoint, string queueToBind, string exchangeName)
        {
            configurer.Register(register => new DelayedMessageExchangeTimeout(endpoint, queueToBind, exchangeName));
        }
    }
}
