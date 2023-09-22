// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace MessageAnalyzer
{
    using Dapr.Client;
    using Dapr.Tests.Common.Models;
    using Google.Protobuf.WellKnownTypes;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Prometheus;
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;

    /// <summary>
    /// Startup class.
    /// </summary>
    public class Startup
    {
        private static readonly Gauge PubSubDuration = Metrics.CreateGauge("lh_message_analyzer_pubsub_duration", "The time between the previous app's publish call and the time this app receives it");

        private static readonly Gauge OutputBindingCallTime = Metrics.CreateGauge("lh_message_analyzer_output_binding_call_time", "The time it takes the binding api to return locally");

        private static readonly Counter BindingApiFailureCount = Metrics.CreateCounter("lh_message_analyzer_binding_failure_count", "Output binding calls that throw");

        private static string[] Sentiments = new string[]
        {
            "verynegative",
            "negative",
            "neutral",
            "positive",
            "verypositive"
        };

        /// <summary>
        /// The name of the pubsub component.  The name of the component and the topic happen to be the same here...
        /// </summary>
        public const string PubsubComponentName = "receivemediapost";

        /// <summary>
        /// The name of the topic to subscribe to.
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

                endpoints.MapPost(PubsubTopicName, ReceiveMediaPost).WithTopic(PubsubComponentName, PubsubTopicName);
            });

            // Receive a "Post" object from the previous app in the pipeline.
            async Task ReceiveMediaPost(HttpContext context, ILogger<Startup> logger)
            {
                logger.LogDebug("Enter ReceiveMediaPost");
                var client = context.RequestServices.GetRequiredService<DaprClient>();

                var message = await JsonSerializer.DeserializeAsync<SocialMediaMessage>(context.Request.Body, serializerOptions);

                // record the time
                TimeSpan durationFromPreviousApp = DateTime.UtcNow - message.PreviousAppTimestamp;                
                PubSubDuration.Set(durationFromPreviousApp.TotalSeconds);

                // update with a sentiment
                message.Sentiment = GenerateRandomSentiment();
                logger.LogInformation("....Invoking binding {BindingName} with message {Message} and sentiment {Sentiment}", BindingName, message.Message, message.Sentiment);

                // overwrite the timestamp so the next app can use it
                message.PreviousAppTimestamp = DateTime.UtcNow;

                try
                {
                    using (OutputBindingCallTime.NewTimer())
                    {
                        await client.InvokeBindingAsync<SocialMediaMessage>(BindingName, "create", message);
                        logger.LogInformation("Invoke binding \"create\" completed successfully");
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Caught {Exception}", e);
                    BindingApiFailureCount.Inc();
                }
            }
        }

        internal string GenerateRandomSentiment()
        {
            Random random = new Random();
            int i = random.Next(Sentiments.Length);
            return Sentiments[i];
        }
    }
}
