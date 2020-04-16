// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.HashTagApp.Controllers
{
    using Dapr.Actors;
    using Dapr.Actors.Client;
    using Dapr.Tests.HashTagApp.Actors;
    using Dapr.Tests.HashTagApp.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Prometheus;
    using System;
    using System.Net.Mime;
    using System.Threading.Tasks;

    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    public class HashTagController : ControllerBase
    {
        private static readonly Gauge BindingDuration = Metrics.CreateGauge("hashtag_counter_binding_duration_in_ms", "The time between the previous app's binding call and the time this app receives it");

        private readonly IConfiguration configuration;

        public HashTagController(IConfiguration config)
        {
            Console.WriteLine("ctor.");
            this.configuration = config;
        }

        [HttpPost("messagebinding")]
        public async Task<IActionResult> PostMessageBinding([FromBody]SocialMediaMessage message)
        {
            Console.WriteLine("enter messagebinding");

            var duration = DateTime.UtcNow - message.PreviousAppTimestamp;
            BindingDuration.Set(duration.TotalSeconds);

            Console.WriteLine($"{message.CreationDate}, {message.CorrelationId}, {message.MessageId}, {message.Message}, {message.Sentiment}");

            int indexOfHash = message.Message.LastIndexOf('#');
            string hashTag = message.Message.Substring(indexOfHash + 1);
            string key = hashTag + "_" + message.Sentiment;

            var actorId = new ActorId(key);
            var proxy = ActorProxy.Create<IHashTagActor>(actorId, "HashTagActor");

            Console.WriteLine($"Increase {key}.");
            try
            {
                await proxy.Increment(key);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e}");
                throw;
            }

            return Ok(new HTTPResponse("Received"));
        }
    }
}
