// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.Snapshot
{
    using Dapr.Actors;
    using Dapr.Actors.Client;
    using Dapr.Client;
    using Dapr.Tests.Common;
    using Dapr.Tests.Common.Models;
    using Dapr.Tests.HashTagApp.Actors;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Prometheus;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    public class Program
    {
        private static readonly Gauge DelaySinceLastSnapShot = Metrics.CreateGauge("lh_snapshot_actor_delay_since_last_snapshot", "The time since the last round of snapshots");

        private static readonly Gauge ActorMethodCallTime = Metrics.CreateGauge("lh_snapshot_actor_method_call_time", "The time it takes for the GetCount actor method to return");

        private static readonly Counter ActorMethodFailureCount = Metrics.CreateCounter("lh_snapshot_actor_method_failure_count", "Actor method calls that throw from snapshot app");


        public static void Main(string[] args)
        {
            ObservabilityUtils.StartMetricsServer();

            var host = CreateHostBuilder(args).Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            int delayInMilliseconds = 5000;
            var delay = Environment.GetEnvironmentVariable("DELAY_IN_MS");
            if (delay != null)
            {
                delayInMilliseconds = int.Parse(delay);
            }

            logger.LogDebug("Configured delayInMilliseconds={DelayInMilliseconds}", delayInMilliseconds);

            Task.Run(() => StartQueryLoopAsync(delayInMilliseconds, logger));

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureTestInfraLogging()
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

        static internal async void StartQueryLoopAsync(int delayInMilliseconds, ILogger<Program> logger)
        {
            logger.LogDebug("Starting query loop");

            TimeSpan delay = TimeSpan.FromMilliseconds(delayInMilliseconds);

            DaprClientBuilder daprClientBuilder = new DaprClientBuilder();
            DaprClient client = daprClientBuilder.Build();

            DateTime lastSnapshotTime = DateTime.MinValue;
            while (true)
            {
                logger.LogInformation("Sleeping '{DelayInMilliseconds}' ms", delayInMilliseconds);
                await Task.Delay(delay);
                Dictionary<string, int> stats = new Dictionary<string, int>();

                if (lastSnapshotTime != DateTime.MinValue)
                {
                    DelaySinceLastSnapShot.Set(DateTime.UtcNow.Subtract(lastSnapshotTime).TotalSeconds);
                }

                foreach (string hashtag in Constants.HashTags)
                {
                    foreach (string sentiment in Constants.Sentiments)
                    {
                        string key = hashtag + "_" + sentiment;
                        var actorId = new ActorId(key);
                        var proxy = ActorProxy.Create<IHashTagActor>(actorId, "HashTagActor");

                        logger.LogInformation("GetCount on {Key}.", key);
                        int count = -1;
                        try
                        {
                            using (ActorMethodCallTime.NewTimer())
                            {
                                count = await proxy.GetCount(key);
                            }

                            stats.Add(key, count);
                            logger.LogInformation("key={Key}, value={Count}.", key, count);
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e, "{Exception}", e);
                            ActorMethodFailureCount.Inc();
                            throw;
                        }
                    }
                }

                lastSnapshotTime = DateTime.UtcNow;

                await client.SaveStateAsync<Dictionary<string, int>>("statestore", "statskey", stats);
            }
        }
    }
}
