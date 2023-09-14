// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

using Prometheus;
using System;

namespace Dapr.Tests.Common {

    public static class ObservabilityUtils {
        public static bool StartMetricsServer()
        {
            // if the env. var. is unset or empty, we enable the metric server
            bool enablePrometheusMetricServer = string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("DISABLE_PROMETHEUS_METRIC_SERVER"));

            if (enablePrometheusMetricServer)
            {
                Console.WriteLine("Starting Prometheus metric server");
                new MetricServer(port: 9988).Start();
            }
            else
            {
                Console.WriteLine("Disabling Prometheus metric server");
            }

            return enablePrometheusMetricServer;
        }
    }

}