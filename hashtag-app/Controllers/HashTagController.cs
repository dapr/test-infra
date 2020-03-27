// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.HashTagApp.Controllers
{
    using System;
    using System.Net.Mime;
    using System.Threading.Tasks;
    using Dapr.Actors;
    using Dapr.Actors.Client;
    using Dapr.Tests.HashTagApp;
    using Dapr.Tests.HashTagApp.Actors;
    using Dapr.Tests.HashTagApp.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    public class HashTagController : ControllerBase
    {
        private readonly ILogger<HashTagController> logger;
        private readonly IConfiguration configuration;

        public HashTagController(ILogger<HashTagController> logger, IConfiguration config)
        {
            this.logger = logger;
            this.configuration = config;
        }

        [HttpPost("messagebinding")]
        public async Task<IActionResult> PostMessageBinding([FromBody]SocialMediaMessage message)
        {
            if (this.configuration[AppSettings.AppType] != AppSettings.HashTagCounter)
            {
                return BadRequest(new HTTPResponse($"Illegal request for {this.configuration[AppSettings.AppType]}"));
            }

            this.logger.LogTrace($"${message.CreationDate}, {message.CorrelationId}, ${message.MessageId}, ${message.Message}, ${message.Sentiment}");

            var actorId = new ActorId(this.configuration[AppSettings.HashTagCounterActorId]);
            var proxy = ActorProxy.Create<IHashTagActor>(actorId, this.configuration[AppSettings.HashTagCounterActorType]);

            this.logger.LogTrace($"Increase ${message.Sentiment}.");
            try
            {
                await proxy.Increment(message.Sentiment);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "messagebinding");
                throw;
            }

            return Ok(new HTTPResponse("Received"));
        }
    }
}
