// ------------------------------------------------------------------------
// Copyright 2025 The Dapr Authors
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ------------------------------------------------------------------------
// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

using Dapr.Tests.Common;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Workflow;
using Microsoft.Extensions.DependencyInjection;
using WorkflowGen.Activities;
using WorkflowGen.Workflows;

namespace WorkflowGen;

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
    public static async Task Main(string[] args)
    {
        ObservabilityUtils.StartMetricsServer();

        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureTestInfraLogging()
            .ConfigureServices(services =>
            {
                services.AddDaprClient();
                services.AddDaprWorkflow(options =>
                {
                    options.RegisterWorkflow<OrderProcessingWorkflow>();
                    options.RegisterActivity<NotifyActivity>();
                    options.RegisterActivity<ReserveInventoryActivity>();
                    options.RegisterActivity<ProcessPaymentActivity>();
                    options.RegisterActivity<UpdateInventoryActivity>();
                });
                services.AddTransient<WorkflowRunner>();
            });

        using var host = builder.Build();

        var workflowRunner = host.Services.GetRequiredService<WorkflowRunner>();
        await using var wTimer =
            new Timer(workflowRunner.Execute!, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));

        await host.RunAsync();
    }
}