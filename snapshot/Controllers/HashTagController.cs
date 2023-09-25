// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.Snapshot.Controllers
{
    using Dapr.Client;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Net.Mime;
    using System.Threading.Tasks;

    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    public class HashTagController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<HashTagController> logger;

        public HashTagController(IConfiguration config, ILogger<HashTagController> logger)
        {
            this.configuration = config;
            this.logger = logger;
        }

        [HttpGet("hashtagdata")]
        public async Task<Dictionary<string, int>> GetHashTagData([FromServices]DaprClient daprClient)
        {
            this.logger.LogDebug("enter GetHashTagData");
            var stats = await daprClient.GetStateAsync<Dictionary<string, int>>("statestore", "statskey");

            return stats;
        }
    }
}
