// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace ValidationWorker
{
    using Dapr.Client;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Prometheus;
    using System;
    using System.Collections.Generic;
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

            Task.Run(() => StartValidationLoopAsync(delayInSeconds));

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

        static internal async void StartValidationLoopAsync(int delayInSeconds)
        {
            const string SnapshotAppName = "snapshot";
            TimeSpan delay = TimeSpan.FromSeconds(delayInSeconds);

            DaprClientBuilder daprClientBuilder = new DaprClientBuilder();
            DaprClient client = daprClientBuilder.Build();

            Dictionary<string, int> prevStats = null;
            Dictionary<string, int> stats = null;

            while (true)
            {
                Console.WriteLine("Checking stats in {0} seconds", delayInSeconds);
                await Task.Delay(delay);

                try
                {
                    using (ServiceInvocationCallTime.NewTimer())
                    {
                        Dictionary<string, string> metadata = new Dictionary<string, string>();
                        metadata.Add("http.verb", "GET");
                        stats = await client.InvokeMethodAsync<Dictionary<string, int>>(SnapshotAppName, "hashtagdata", metadata);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Caught {0}", e.ToString());
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

                    Console.WriteLine("Number changed is {0}", changed);
                    if (changed == 0)
                    {
                        LogStats(prevStats, stats);
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

        static internal void LogStats(Dictionary<string, int> prevStats, Dictionary<string, int> stats)
        {
            Console.WriteLine("The stats from the snapshot app did not change reporting error metric, logging previous and current:");
            Console.WriteLine("Previous:");
            foreach (var kv in prevStats)
            {
                Console.WriteLine("{0} - {1}", kv.Key, kv.Value);
            }

            Console.WriteLine("Current:");
            foreach (var kv in stats)
            {
                Console.WriteLine("{0} - {1}", kv.Key, kv.Value);
            }
        }
    }
}
