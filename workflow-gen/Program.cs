using System.Globalization;
// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

using Dapr.Client;
using Dapr.Tests.Common;
using Dapr.Workflow;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WorkflowGen.Activities;
using WorkflowGen.Models;
using WorkflowGen.Workflows;

namespace WorkflowGen
{
    /// <summary>
    /// WorkflowGenerator - runs worflows and executes them using Dapr.
    /// The main functionality is in StartWorkflowGeneratorAsync().
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main for WorkflowGen
        /// </summary>
        /// <param name="args">Arguments.</param>
        public static void Main(string[] args)
        {
            ObservabilityUtils.StartMetricsServer();

            var builder = Host.CreateDefaultBuilder(args)
                .ConfigureTestInfraLogging()
                .ConfigureServices(services =>
                {
                    services.AddDaprWorkflow(options =>
                    {
                        options.RegisterWorkflow<OrderProcessingWorkflow>();
                        options.RegisterActivity<NotifyActivity>();
                        options.RegisterActivity<ReserveInventoryActivity>();
                        options.RegisterActivity<ProcessPaymentActivity>();
                        options.RegisterActivity<UpdateInventoryActivity>();
                    });
                });

            using var host = builder.Build();

            var logger = host.Services.GetRequiredService<ILogger<WorkflowRunner>>();

            var wTimer = StartExecutingWorkflows(30, logger);
            host.Run();
            wTimer.Dispose();
        }

        static internal Timer StartExecutingWorkflows(int periodInSeconds, ILogger<WorkflowRunner> logger)
        {
            DaprClientBuilder daprClientBuilder = new DaprClientBuilder();
            var client = new DaprClientBuilder().Build();
            var counter = 0;
            var workflowRunner = new WorkflowRunner(client, counter, logger);

            return new Timer(workflowRunner.Execute, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(periodInSeconds));
        }
    }
}

