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

using Microsoft.AspNetCore.Mvc;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Threading;

namespace PubsubWorkflow.Services
{
    public class MetricsService : IDisposable
    {
        private static readonly Gauge GlacialMessageDelay = Metrics.CreateGauge("lh_pubsub_workflow_glacial_delay", "The time between now and when the last glacial message was received (max 12 hours)");
        private static readonly Gauge SlowMessageDelay = Metrics.CreateGauge("lh_pubsub_workflow_slow_delay", "The time between now and when the last slow message was received (max 1 hour)");
        private static readonly Gauge MediumMessageDelay = Metrics.CreateGauge("lh_pubsub_workflow_medium_delay", "The time between now and when the last medium message was received (max 5 minutes)");
        private static readonly Gauge RapidMessageDelay = Metrics.CreateGauge("lh_pubsub_workflow_rapid_delay", "The time between now and when the last rapid message was received (max 10 seconds)");

        private readonly Timer _metricTimer;
        private readonly IDictionary<PubsubRates, DateTime> _lastMessageReceived;
        public MetricsService([FromServices] IDictionary<PubsubRates, DateTime> lastReceivedDict)
        {
            _lastMessageReceived = lastReceivedDict;
            _metricTimer = new Timer(UpdateMetrics, null, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(5));
        }

        public void MarkMessageReceived(PubsubRates rate)
        {
            _lastMessageReceived[rate] = DateTime.UtcNow;
        }

        internal void UpdateMetrics(object stateInfo)
        {
            foreach (var entry in _lastMessageReceived)
            {
                switch (entry.Key)
                {
                    case PubsubRates.Glacial:
                        GlacialMessageDelay.Set((DateTime.UtcNow - entry.Value).TotalSeconds);
                        break;
                    case PubsubRates.Slow:
                        SlowMessageDelay.Set((DateTime.UtcNow - entry.Value).TotalSeconds);
                        break;
                    case PubsubRates.Medium:
                        MediumMessageDelay.Set((DateTime.UtcNow - entry.Value).TotalSeconds);
                        break;
                    case PubsubRates.Rapid:
                        RapidMessageDelay.Set((DateTime.UtcNow - entry.Value).TotalSeconds);
                        break;
                }
            }
        }

        public void Dispose()
        {
            _metricTimer?.Dispose();
        }
    }
}
