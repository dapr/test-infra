using System.Globalization;
// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

using Dapr.Client;
using Dapr.Workflow;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
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
            var server = new MetricServer(port: 9988);
            server.Start();

            var builder = Host.CreateDefaultBuilder(args).ConfigureServices(services =>
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

            var wTimer = StartExecutingWorkflows(15);
            using var host = builder.Build();
            host.Run();
            wTimer.Dispose();

        }

        static internal Timer StartExecutingWorkflows(int periodInSeconds)
        {
            DaprClientBuilder daprClientBuilder = new DaprClientBuilder();
            var client = new DaprClientBuilder().Build();
            var counter = 0;
            var workflowRunner = new WorkflowRunner(client, counter);

            return new Timer(workflowRunner.Execute, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(periodInSeconds));
        }

    }
}

