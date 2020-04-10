// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.Snapshot.Controllers
{
    using Dapr.Client;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Net.Mime;
    using System.Threading.Tasks;

    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    public class HashTagController : ControllerBase
    {
        private readonly IConfiguration configuration;

        public HashTagController(IConfiguration config)
        {
            Console.WriteLine("ctor.");
            this.configuration = config;
        }

        [HttpGet("hashtagdata")]
        public async Task<Dictionary<string, int>> GetHashTagData([FromServices]DaprClient daprClient)
        {
            Console.WriteLine("enter GetHashTagData");
            var stats = await daprClient.GetStateAsync<Dictionary<string, int>>("statestore", "statskey");

            return stats;
        }
    }
}
