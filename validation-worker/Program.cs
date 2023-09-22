// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace ValidationWorker
{
    using Dapr.Client;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Prometheus;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// ValidationWorker - uses Dapr service invocation to get stats from the snapshot app.  Counts differences in stats.
    /// Reports metrics if there is no change.    
    /// </summary>
    public class Program
    {
        private static readonly Gauge ServiceInvocationCallTime = Metrics.CreateGauge("lh_validation_worker_service_invocation_call_time", "The time it takes the Dapr service invocation call to return");
        private static readonly Gauge UnchangedStatsMetric = Metrics.CreateGauge("lh_validation_worker_stats_unchanged", "Signalled when stats queries from the snapshot service have not changed");

        private static readonly Counter ServiceInvocationFailureCount = Metrics.CreateCounter("lh_validation_worker_service_invocation_failure_count", "Dapr service invocation calls that throw");

        /// <summary>
        /// Main for ValidationWorker
        /// </summary>
        /// <param name="args">Arguments.</param>
        public static void Main(string[] args)
        {
            int delayInSeconds = 60;
            var delay = Environment.GetEnvironmentVariable("DELAY_IN_SEC");
            if (delay != null)
            {
                delayInSeconds = int.Parse(delay);
            }

            var server = new MetricServer(port: 9988);
            server.Start();

            IHost host = CreateHostBuilder(args).Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            Task.Run(() => StartValidationLoopAsync(delayInSeconds, logger));

            host.Run();
        }

        /// <summary>
        /// Creates WebHost Builder.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <returns>Returns IHostbuilder.</returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        static internal async void StartValidationLoopAsync(int delayInSeconds, ILogger<Program> logger)
        {
            const string SnapshotAppName = "snapshot";
            TimeSpan delay = TimeSpan.FromSeconds(delayInSeconds);

            DaprClientBuilder daprClientBuilder = new DaprClientBuilder();
            DaprClient client = daprClientBuilder.Build();

            Dictionary<string, int> prevStats = null;
            Dictionary<string, int> stats = null;

            while (true)
            {
                logger.LogInformation("Checking stats in {DelayInSeconds} seconds", delayInSeconds);
                await Task.Delay(delay);

                try
                {
                    using (ServiceInvocationCallTime.NewTimer())
                    {
                        var request = client.CreateInvokeMethodRequest(SnapshotAppName, "hashtagdata");
                        request.Method = HttpMethod.Get;
                        
                        stats = await client.InvokeMethodAsync<Dictionary<string, int>>(request);
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Caught {Exception}", e);
                    ServiceInvocationFailureCount.Inc();
                    continue;
                }

                // skip the calculation the first time
                if (prevStats != null)
                {
                    int changed = 0;
                    foreach (var kv in stats)
                    {
                        if (prevStats.ContainsKey(kv.Key) == false
                            || prevStats[kv.Key] != kv.Value)
                        {
                            changed++;
                        }
                    }

                    logger.LogInformation("Number changed is {Changed}", changed);
                    if (changed == 0)
                    {
                        LogStats(prevStats, stats, logger);
                        UnchangedStatsMetric.IncTo(1);
                    }
                    else
                    {
                        UnchangedStatsMetric.IncTo(0);
                    }
                }

                prevStats = stats;
            }
        }

        static internal void LogStats(Dictionary<string, int> prevStats, Dictionary<string, int> stats, ILogger<Program> logger)
        {
            logger.LogInformation("The stats from the snapshot app did not change reporting error metric, logging previous and current:");
            logger.LogInformation("Previous:");
            foreach (var kv in prevStats)
            {
                logger.LogInformation("{Key} - {Value}", kv.Key, kv.Value);
            }

            logger.LogInformation("Current:");
            foreach (var kv in stats)
            {
                logger.LogInformation("{Key} - {Value}", kv.Key, kv.Value);
            }
        }
    }
}
