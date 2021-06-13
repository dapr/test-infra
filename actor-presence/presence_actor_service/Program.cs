// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.Actors.PresenceTest
{
    using Dapr.Actors;
    using Dapr.Actors.AspNetCore;
    using Dapr.Actors.Runtime;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;

    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseActors(actorRuntime =>
                {
                    actorRuntime.RegisterActor<PlayerActor>();
                    actorRuntime.RegisterActor<GameActor>();
                    actorRuntime.RegisterActor<PresenceActor>();
                });
    }
}
