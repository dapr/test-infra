// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.HashTagApp.Actors
{
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
        /// <param name="service">Actor Service hosting the actor.</param>
        /// <param name="actorId">Actor Id.</param>
        public HashTagActor(ActorService service, ActorId actorId)
            : base(service, actorId)
        {
            // TODO: ActorService may need to have IHostBuilder reference to allow user to interact web host.
            // For example, getting logger factory given by WebHost
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddConsole();
            });

            this.logger = loggerFactory.CreateLogger<HashTagActor>();
        }

        /// <inheritdoc/>
        public async Task Increment(string sentiment)
        {
            int count = 0;

            try 
            {
                count = await this.StateManager.GetStateAsync<int>(sentiment);
            }
            catch (KeyNotFoundException)
            {
                this.logger.LogInformation($"{sentiment} does not exist. {sentiment} will be initialized to 0.");
            }

            this.logger.LogInformation($"{sentiment} = {count}");
            count++;
            await this.StateManager.SetStateAsync<int>(sentiment, count);
            this.logger.LogInformation($"Increment {sentiment}.");
        }
    }
}
