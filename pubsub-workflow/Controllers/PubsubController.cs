// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace PubsubWorkflow
{
    [ApiController]
    public class PubsubController : ControllerBase
    {
        internal static DateTime lastRapidCall = DateTime.Now;
        internal static DateTime lastMediumCall = DateTime.Now;
        internal static DateTime lastSlowCall = DateTime.Now;
        internal static DateTime lastGlacialCall = DateTime.Now;

        private readonly ILogger<PubsubController> logger;

        public PubsubController(ILogger<PubsubController> logger)
        {
            this.logger = logger;
        }

        [Topic("longhaul-sb-rapid", "rapidtopic")]
        [HttpPost("rapidMessage")]
        public IActionResult RapidMessageHandler() {
            var lastHit = lastRapidCall;
            lastRapidCall = DateTime.Now;
            this.logger.LogInformation("Rapid subscription hit at {LastRapidCall}, previous hit at {LastHit}", lastRapidCall, lastHit);
            return Ok();
        }

        [Topic("longhaul-sb-medium", "mediumtopic")]
        [HttpPost("mediumMessage")]
        public IActionResult MediumMessageHandler() {
            var lastHit = lastMediumCall;
            lastMediumCall = DateTime.Now;
            this.logger.LogInformation("Medium subscription hit at {LastMediumCall}, previous hit at {LastHit}", lastMediumCall, lastHit);
            return Ok();
        }

        [Topic("longhaul-sb-slow", "slowtopic")]
        [HttpPost("slowMessage")]
        public IActionResult SlowMessageHandler() {
            var lastHit = lastSlowCall;
            lastSlowCall = DateTime.Now;
            this.logger.LogInformation("Slow subscription hit at {LastSlowCall}, previous hit at {LastHit}", lastSlowCall, lastHit);
            return Ok();
        }
        
        [Topic("longhaul-sb-glacial", "glacialtopic")]
        [HttpPost("glacialMessage")]
        public IActionResult GlacialMessageHandler() {
            var lastHit = lastGlacialCall;
            lastGlacialCall = DateTime.Now;
            this.logger.LogInformation("Glacial subscription hit at {LastGlacialCall}, previous hit at {LastHit}", lastGlacialCall, lastHit);
            return Ok();
        }
    }
}