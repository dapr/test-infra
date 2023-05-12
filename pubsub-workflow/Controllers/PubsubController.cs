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

using Dapr;
using Microsoft.AspNetCore.Mvc;
using PubsubWorkflow.Services;
using System;

namespace PubsubWorkflow.Controllers
{
    [ApiController]
    public class PubsubController : ControllerBase
    {
        internal static DateTime lastRapidCall = DateTime.Now;
        internal static DateTime lastMediumCall = DateTime.Now;
        internal static DateTime lastSlowCall = DateTime.Now;
        internal static DateTime lastGlacialCall = DateTime.Now;

        [Topic(Constants.RapidPubsubName, Constants.RapidTopic)]
        [HttpPost("rapidMessage")]
        public IActionResult RapidMessageHandler([FromServices] MetricsService metricsService)
        {
            var lastHit = lastRapidCall;
            lastRapidCall = DateTime.Now;
            Console.WriteLine($"Rapid subscription hit at {lastRapidCall}, previous hit at {lastHit}");
            metricsService.MarkMessageReceived(PubsubRates.Rapid);
            return Ok();
        }

        [Topic(Constants.MediumPubsubName, Constants.MediumTopic)]
        [HttpPost("mediumMessage")]
        public IActionResult MediumMessageHandler([FromServices] MetricsService metricsService)
        {
            var lastHit = lastMediumCall;
            lastMediumCall = DateTime.Now;
            Console.WriteLine($"Medium subscription hit at {lastMediumCall}, previous hit at {lastHit}");
            metricsService.MarkMessageReceived(PubsubRates.Medium);
            return Ok();
        }

        [Topic(Constants.SlowPubsubName, Constants.SlowTopic)]
        [HttpPost("slowMessage")]
        public IActionResult SlowMessageHandler([FromServices] MetricsService metricsService)
        {
            var lastHit = lastSlowCall;
            lastSlowCall = DateTime.Now;
            Console.WriteLine($"Slow subscription hit at {lastSlowCall}, previous hit at {lastHit}");
            metricsService.MarkMessageReceived(PubsubRates.Slow);
            return Ok();
        }

        [Topic(Constants.GlacialPubsubName, Constants.GlacialTopic)]
        [HttpPost("glacialMessage")]
        public IActionResult GlacialMessageHandler([FromServices] MetricsService metricsService)
        {
            var lastHit = lastGlacialCall;
            lastGlacialCall = DateTime.Now;
            Console.WriteLine($"Glacial subscription hit at {lastGlacialCall}, previous hit at {lastHit}");
            metricsService.MarkMessageReceived(PubsubRates.Glacial);
            return Ok();
        }
    }
}