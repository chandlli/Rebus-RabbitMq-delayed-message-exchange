using Rebus.Config;
using Rebus.Logging;
using Rebus.Subscriptions;
using Rebus.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Transport.Sample
{
    public static class MyTransportExtension
    {
        public static void UserMytransport(this StandardConfigurer<ITransport> configurer, string endpoint, string queue
            , string delayExchange)
        {
            configurer
                .OtherService<MyTransport>()
                .Register(c =>
                {
                    return new MyTransport("amqp://localhost", "Messages", c.Get<IRebusLoggerFactory>(), delayExchangeName: delayExchange);
                });

            configurer
                .OtherService<ISubscriptionStorage>()
                .Register(c => c.Get<MyTransport>(), description: "My Transport");

            configurer.Register(c => c.Get<MyTransport>());
        }
    }
}
