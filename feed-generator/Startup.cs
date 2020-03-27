// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace FeedGenerator
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System.Text.Json;

    /// <summary>
    /// Startup class.  This is a stub and being left in case we want to modify this 
    /// app to start/stop sending messages on receiving a call on an endpoint.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configures Services.
        /// </summary>
        /// <param name="services">Service Collection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDaprClient();

            services.AddSingleton(new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
            });
        }     

        /// <summary>
        /// Configures Application Builder and WebHost environment.
        /// </summary>
        /// <param name="app">Application builder.</param>
        /// <param name="env">Webhost environment.</param>
        /// <param name="serializerOptions">Options for JSON serialization.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, JsonSerializerOptions serializerOptions)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseCloudEvents();

            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapSubscribeHandler();

                //endpoints.MapPost("callhana", CallHana).WithTopic("callhana");

                // call self for now
                //endpoints.MapPost("receivemediapost", ReceiveMediaPost).WithTopic("receivemediapost");
            });

            //async Task CallHana(HttpContext context)
            //{
            //    Console.WriteLine("Enter CallHana");
            //    var client = context.RequestServices.GetRequiredService<DaprClient>();

            //    var data = await JsonSerializer.DeserializeAsync<Post>(context.Request.Body, serializerOptions);

            //    string jsonString;
            //    JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };
            //    jsonString = JsonSerializer.Serialize(data, options);
            //    Console.WriteLine(".....content as json is '{0}'", jsonString);

            //    Console.WriteLine(".....publishing");
            //    await client.PublishEventAsync<Post>("receivemediapost", data);
            //    Console.WriteLine(".....done publishing");
            //}
        }
    }
}
