// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

using Dapr.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PubsubWorkflow
{
    class PubsubWorkflow
    {
        private static string pubsubName = "longhaul-sb";

        static void Main(string[] args)
        {
            Console.WriteLine("Starting Pubsub Workflow");

            var server = new MetricServer(port: 9988);
            server.Start();

            var host = CreateHostBuilder(args).Build();

            var rapidTimer = StartPublishingMessages(10, pubsubName, "rapidtopic");
            var mediumTimer = StartPublishingMessages(300, pubsubName, "mediumtopic");
            var slowTimer = StartPublishingMessages(3600, pubsubName, "slowtopic");
            
            host.Run();

            Console.WriteLine("Exiting Pubsub Workflow");

            rapidTimer.Dispose();
            mediumTimer.Dispose();
            slowTimer.Dispose();
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

        static internal Timer StartPublishingMessages(int periodInSeconds, string pubsubName, string topic)
        {
            var client = new DaprClientBuilder().Build();
            var messagePublisher = new MessagePublisher(client, pubsubName, topic);

            return new Timer(messagePublisher.Publish, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(periodInSeconds));
        }
    }
}
