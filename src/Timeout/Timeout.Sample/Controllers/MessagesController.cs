using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rebus.Bus;

namespace Timeout.Sample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly IBus bus;

        public MessagesController(IBus bus)
        {
            this.bus = bus;
        }

        [HttpGet("happy")]
        public IActionResult HappyMessage()
        {
            bus.Publish(new HappyMessage($"This is a happy message :) at {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}"));

            return Ok("Published happy message");
        }

        [HttpGet("sad")]
        public IActionResult SadMessage()
        {
            bus.Publish(new SadMessage($"This is a sad message :( at {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}"));

            return Ok("Published sad message");
        }
    }
}