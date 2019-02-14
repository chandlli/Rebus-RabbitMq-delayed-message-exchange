using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;
using Rebus.Config;
using Rebus.ServiceProvider;

namespace Timeout.Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AutoRegisterHandlersFromAssemblyOf<HandlerMessages>();

            services.AddRebus(config => config
                    .Transport(transport => transport.UseRabbitMq("amqp://localhost", "MyQueue"))
                    .Timeouts(timeout => timeout.UseRabbitMqDelayed("amqp://localhost", "MyQueue", "delayed.topic")));


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IBus bus)
        {
            var types = GetType().Assembly.GetTypes();

            bus.Subscribe<HappyMessage>();
            bus.Subscribe<SadMessage>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRebus()
                .UseMvc();
        }
    }
}
