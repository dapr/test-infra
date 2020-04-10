// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.Snapshot
{
    using Dapr.Actors;
    using Dapr.Actors.Client;
    using Dapr.Client;
    using Dapr.Tests.HashTagApp.Actors;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    public class Program
    {
        // This uses the names of shapes for a generic theme
        static internal string[] HashTags = new string[]
        {
            "circle",
            "ellipse",
            "square",
            "rectangle",
            "triangle",
            "star",
            "cardioid",
            "picycloid",
            "limocon",
            "hypocycoid"
        };

        private static string[] Sentiments = new string[]
        {
            "verynegative",
            "negative",
            "neutral",
            "strong",
            "verystrong"
        };

        public static void Main(string[] args)
        {
            int delayInMilliseconds = 5000;
            var delay = Environment.GetEnvironmentVariable("DELAY_IN_MS");
            if (delay != null)
            {
                delayInMilliseconds = int.Parse(delay);
            }

            Console.WriteLine("Configured delayInMilliseconds={0}", delayInMilliseconds);
            
            var host = CreateHostBuilder(args).Build();

            Task.Run(() => StartQueryLoopAsync(delayInMilliseconds));

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
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

                    var host = webBuilder.UseStartup<Startup>()
                        .UseUrls(urls: $"http://*:{appSettings[AppSettings.DaprHTTPAppPort]}");
                });

            return hostBuilder;
        }

        static internal async void StartQueryLoopAsync(int delayInMilliseconds)
        {
            Console.WriteLine("Starting query loop");

            TimeSpan delay = TimeSpan.FromMilliseconds(delayInMilliseconds);

            DaprClientBuilder daprClientBuilder = new DaprClientBuilder();
            DaprClient client = daprClientBuilder.Build();

            while (true)
            {
                Console.WriteLine("Sleeping '{0}' ms", delayInMilliseconds);
                await Task.Delay(delay);
                Dictionary<string, int> stats = new Dictionary<string, int>();

                foreach (string hashtag in HashTags)
                {
                   
                    foreach (string sentiment in Sentiments)
                    {
                        string key = hashtag + "_" + sentiment;
                        var actorId = new ActorId(key);
                        var proxy = ActorProxy.Create<IHashTagActor>(actorId, "HashTagActor");

                        Console.WriteLine($"GetCount on {key}.");
                        int count = -1;
                        try
                        {
                            count = await proxy.GetCount(key);
                            stats.Add(key, count);
                            Console.WriteLine($"key={key}, value={count}.");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"{e}");
                            throw;
                        }
                    }
                }

                await client.SaveStateAsync<Dictionary<string, int>>("statestore", "statskey", stats);                              
            }
        }
    }
}
