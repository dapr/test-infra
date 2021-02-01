// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.HashTagApp
{
    using System;
    using System.IO;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Prometheus;

    public class Program
    {
        public static void Main(string[] args)
        {
            var server = new MetricServer(port: 9988);
            server.Start();

           
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) {
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, config) =>
                {
                    config.ClearProviders();
                    config.AddConsole();

                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var appSettings = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)                        
                        .AddCommandLine(args)
                        .Build();

                    var host = webBuilder.UseStartup<Startup>()
                        .UseUrls(urls: $"http://*:{appSettings[AppSettings.DaprHTTPAppPort]}");
                });

            return hostBuilder;
        }
    }
}
