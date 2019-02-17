using Rebus.Bus;
using Rebus.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Transport.Sample
{
    public class HandlerMessages : IHandleMessages<HappyMessage>, IHandleMessages<SadMessage>
    {
        private readonly IBus bus;

        public HandlerMessages(IBus bus)
        {
            this.bus = bus;
        }

        public async Task Handle(HappyMessage message)
        {
            await bus.DeferLocal(TimeSpan.FromSeconds(15), message);
        }

        public async Task Handle(SadMessage message)
        {
            await bus.DeferLocal(TimeSpan.FromSeconds(10), message);
        }
    }
}
