// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.Snapshot.Controllers
{
    using Dapr.Actors;
    using Dapr.Actors.Client;
    using Dapr.Client;
    using Dapr.Tests.HashTagApp.Actors;
    using Dapr.Tests.HashTagApp.Models;
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

        public HashTagController(IConfiguration config)
        {
            Console.WriteLine("ctor.");
            this.configuration = config;
        }

      
      
        //[HttpPost("messagebinding")]
        [HttpGet("hashtagdata")]
        public async Task<Dictionary<string,int>> GetHashTagData([FromServices]DaprClient daprClient)
        {
            Console.WriteLine("enter GetHashTagData");

            //Console.WriteLine($"{message.CreationDate}, {message.CorrelationId}, {message.MessageId}, {message.Message}, {message.Sentiment}");

            //ZZZZ  - fake
            //Dictionary<string, int> stats = new Dictionary<string, int>();
            //stats.Add("cat", 3);
            //stats.Add("dog", 2);
            //await Task.Delay(1);
            //await daprClient.SaveStateAsync<Dictionary<string,int>>("statestore", "mykey", stats);


            var stats = await daprClient.GetStateAsync<Dictionary<string, int>>("statestore", "statskey");

            return stats;
        }
    }
}
