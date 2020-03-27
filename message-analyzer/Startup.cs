// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace MessageAnalyzer
{
    using Dapr.Client;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;

    /// <summary>
    /// Startup class.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// 
        /// </summary>
        public const string PubsubTopicName = "receivemediapost";

        /// <summary>
        /// 
        /// </summary>
        public const string BindingName = "messagebinding";

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
                endpoints.MapSubscribeHandler();

                endpoints.MapPost(PubsubTopicName, ReceiveMediaPost).WithTopic(PubsubTopicName);
            });

            // Receive a "Post" object from the previous app in the pipeline.
            async Task ReceiveMediaPost(HttpContext context)
            {
                Console.WriteLine("Enter ReceiveMediaPost");
                var client = context.RequestServices.GetRequiredService<DaprClient>();

                var data = await JsonSerializer.DeserializeAsync<Post>(context.Request.Body, serializerOptions);

                PostWithSentiment postWithSentiment = new PostWithSentiment(data)
                {
                    Sentiment = GenerateRandomSentiment()
                };

                await client.InvokeBindingAsync<PostWithSentiment>(BindingName, postWithSentiment);
            }
        }

        internal int GenerateRandomSentiment()
        {
            Random random = new Random();
            return random.Next(1, 6);
        }
    }
}
