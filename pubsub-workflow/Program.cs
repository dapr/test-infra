/*
Copyright 2021 The Dapr Authors
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
    http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using PubsubWorkflow.Services;
using System;
using System.IO;
using System.Threading;

namespace PubsubWorkflow
{

    class PubsubWorkflow
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Pubsub Workflow");

            var server = new MetricServer(port: 9988);
            server.Start();

            var host = CreateHostBuilder(args).Build();
            var messagePublishingService = host.Services.GetRequiredService<MessagePublishingService>();
            var rapidTimer = StartPublishingMessages(messagePublishingService, Constants.RapidDelaySeconds, Constants.RapidPubsubName, Constants.RapidTopic);
            var mediumTimer = StartPublishingMessages(messagePublishingService, Constants.MediumDelaySeconds, Constants.MediumPubsubName, Constants.MediumTopic);
            var slowTimer = StartPublishingMessages(messagePublishingService, Constants.SlowDelaySeconds, Constants.SlowPubsubName, Constants.SlowTopic);
            var glacialTimer = StartPublishingMessages(messagePublishingService, Constants.GlacialDelaySeconds, Constants.GlacialPubsubName, Constants.GlacialTopic);

            host.Run();

            Console.WriteLine("Exiting Pubsub Workflow");

            rapidTimer.Dispose();
            mediumTimer.Dispose();
            slowTimer.Dispose();
            glacialTimer.Dispose();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, config) =>
                    {
                        config.ClearProviders();
                        config.AddConsole();

                    })
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        var appSettings = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)
                            .AddCommandLine(args)
                            .Build();

                        webBuilder.UseStartup<Startup>()
                            .UseUrls(urls: $"http://*:{appSettings["DaprHTTPAppPort"]}");
                    });

        static internal Timer StartPublishingMessages(MessagePublishingService publishingService, int periodInSeconds, string pubsubName, string topic)
        {
            var request = new PublishRequest()
            {
                PubsubName = pubsubName,
                Topic = topic
            };
            return new Timer(publishingService.Publish, request, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(periodInSeconds));
        }
    }
}
