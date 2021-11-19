// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Dapr.Client;
using Dapr.Tests.Common.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace BindingWorkflow
{
    public class Program
    {
        private static readonly string AzureServiceBusBinding = "longhaul-invoke-binding";
        private static readonly string BlobStorageBinding = "longhaul-blob-binding";
        private static readonly string CosmosDBBinding = "longhaul-cosmosdb-binding";
        private static readonly string CreateOperation = "create";

        public static void Main(string[] args)
        {
            Console.WriteLine("Starting bindings workflow.");

            var server = new MetricServer(port: 9988);
            server.Start();

            var host = CreateHostBuilder(args).Build();

            // Invoke the blob storage output binding 200 times every 5 minutes (2400 messages per hour).
            var blobPublishTimer = StartInvokingBinding(300, BlobStorageBinding, CreateOperation, 200, new Dictionary<string, string>());
            var cosmosDBPublishTimer = StartInvokingBinding(300, CosmosDBBinding, CreateOperation, 200, new Dictionary<string, string>() {
                { "autoGenId", "true"}
            });

            host.Run();

            Console.WriteLine("Exiting bindings workflow.");

            blobPublishTimer.Dispose();
            cosmosDBPublishTimer.Dispose();
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

        static internal Timer StartInvokingBinding(int periodInSeconds, string targetBinding, string targetOperation, int invocationCount, Dictionary<string, string> metadata)
        {
            var client = new DaprClientBuilder().Build();

            return new Timer(async (state) =>
            {
                Console.WriteLine($"Invoking binding to trigger {targetBinding} binding.");
                var data = new Dictionary<string, string>();
                data.Add("timestamp", DateTime.Now.ToLongTimeString());
                data.Add("pk", Guid.NewGuid().ToString());
                var request = new BindingInvocationMessage
                {
                    TargetBinding = targetBinding,
                    TargetOperation = targetOperation,
                    InvocationCount = invocationCount,
                    Data = data,
                    Metadata = metadata,
                };
                await client.InvokeBindingAsync(AzureServiceBusBinding, CreateOperation, request);
            }, null, TimeSpan.FromSeconds(new Random().Next(5, 10)), TimeSpan.FromSeconds(periodInSeconds));
        }
    }
}
