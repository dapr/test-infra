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

using Dapr.Client;
using Prometheus;
using System;
using Dapr.Workflow;
using WorkflowGen.Models;
using WorkflowGen.Workflows;
using Microsoft.Extensions.Logging;

namespace WorkflowGen;

internal sealed partial class WorkflowRunner(ILogger<WorkflowRunner> logger, DaprClient daprClient, DaprWorkflowClient workflowClient)
{
    private static readonly Gauge ExecutionCallTime = Metrics.CreateGauge("lh_workflow_generator_execution_call_time", "The time it takes for the workflow call to return");
    private static readonly Counter ExecutionFailureCount = Metrics.CreateCounter("lh_workflow_generator_execution_failure_count", "Publich calls that throw");
    private static readonly Random Random = new();

    private const string StateStoreName = "statestore";
    private int _counter = 0;

    internal async void Execute(object stateInfo)
    {
        if (_counter == 0)
        {
            logger.LogInformation("Restocking inventory...");
            await daprClient.SaveStateAsync(StateStoreName, "Cars", new OrderPayload(Name: "Cars", TotalCost: 15000, Quantity: 100));
        }
        logger.LogInformation("Executing workflow...");
        var num = Random.Next(40);
        var orderInfo = new OrderPayload("Cars", num * 1000, num);
        var orderId = Guid.NewGuid().ToString()[..8];
        using (ExecutionCallTime.NewTimer())
        {
            try
            {
                await workflowClient.ScheduleNewWorkflowAsync(
                    nameof(OrderProcessingWorkflow), orderId, orderInfo);

                var state = await workflowClient.WaitForWorkflowStartAsync(orderId);

                LogWorkflowStarted(logger, state.RuntimeStatus);

                state = await workflowClient.WaitForWorkflowCompletionAsync(orderId);

                LogWorkflowStatus(logger, state.RuntimeStatus);
            }
            catch (Exception ex)
            {
                LogWorkflowException(logger, ex);
                ExecutionFailureCount.Inc();
            }
        }
        _counter++;
        if (_counter > 4)
        {
            _counter = 0;
        }
    }

    [LoggerMessage(LogLevel.Information, "Your workflow has started. Here is the status of the workflow: {status}")]
    static partial void LogWorkflowStarted(ILogger logger, WorkflowRuntimeStatus status);

    [LoggerMessage(LogLevel.Information, "Workflow Status: {status}")]
    static partial void LogWorkflowStatus(ILogger logger, WorkflowRuntimeStatus status );

    [LoggerMessage(LogLevel.Error, "Caught {exception}")]
    static partial void LogWorkflowException(ILogger logger, Exception exception);
}