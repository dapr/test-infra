// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.HashTagApp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Dapr.Actors.AspNetCore;
    using Dapr.Tests.HashTagApp.Actors;

    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>()
                    // TODO: Make app port configurable by appsetting.json
                    .UseUrls(urls: "http://*:3000")
                    .UseActors(actorRuntime =>
                    {
                        // Register HashTagActor ActorType
                        actorRuntime.RegisterActor<HashTagActor>();
                    });
            });
    }
}
