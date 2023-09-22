// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.HashTagApp.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Dapr.Actors;
    using Dapr.Actors.Runtime;
    using Microsoft.Extensions.Logging;

    public class HashTagActor : Actor, IHashTagActor
    {
        private readonly ILogger<HashTagActor> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HashTagActor"/> class.
        /// </summary>
        /// <param name="host">Actor Service hosting the actor.</param>
        public HashTagActor(ActorHost host, ILogger<HashTagActor> logger)
            : base(host)
        {
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task Increment(string hashtagAndSentiment)
        {
            int count = 0;

            try 
            {
                count = await this.StateManager.GetStateAsync<int>(hashtagAndSentiment);
            }
            catch (KeyNotFoundException)
            {
                this.logger.LogDebug("{HashtagAndSentiment} does not exist. {HashtagAndSentiment} will be initialized to 0.", hashtagAndSentiment);
            }

            this.logger.LogDebug("{HashtagAndSentiment} = {Count}", hashtagAndSentiment, count);
            count++;
            await this.StateManager.SetStateAsync<int>(hashtagAndSentiment, count);
            this.logger.LogInformation("Incremented {HashtagAndSentiment}.", hashtagAndSentiment);
        }

        public async Task<int> GetCount(string hashtagAndSentiment)
        {
            int count = -1;
            try
            {
                count = await this.StateManager.GetStateAsync<int>(hashtagAndSentiment);
                this.logger.LogInformation("GetCount for {HashtagAndSentiment} found and it is {Count}.", hashtagAndSentiment, count);
            }
            catch (KeyNotFoundException)
            {
                this.logger.LogInformation("{HashtagAndSentiment} does not exist.", hashtagAndSentiment);
            }

            return count;
        }
    }
}
