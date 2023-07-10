// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Prometheus;
using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowGen.Activities;
using WorkflowGen.Models;
using WorkflowGen.Workflows;

namespace WorkflowGen
{
    public class WorkflowRunner
    {

        private static readonly Gauge ExecutionCallTime = Metrics.CreateGauge("lh_workflow_generator_execution_call_time", "The time it takes for the workflow call to return");

        private static readonly Counter ExecutionFailureCount = Metrics.CreateCounter("lh_workflow_generator_execution_failure_count", "Publich calls that throw");

        const string DaprWorkflowComponent = "dapr";
        private readonly DaprClient Client;
        int Counter;

        public WorkflowRunner([FromServices] DaprClient client, int counter)
        {
            this.Client = client;
            this.Counter = counter;
        }

        internal async void Execute(Object stateInfo)
        {
            if (Counter == 0)
            {
                Console.WriteLine("Restocking inventory...");
                await Client.SaveStateAsync<OrderPayload>("statestore", "Cars", new OrderPayload(Name: "Cars", TotalCost: 15000, Quantity: 100));
            }
            Console.WriteLine("Executing workflow...");
            Random random = new Random();
            var num = random.Next(40);
            OrderPayload orderInfo = new OrderPayload("Cars", num * 1000, num);
            string orderId = Guid.NewGuid().ToString()[..8];
            using (ExecutionCallTime.NewTimer())
            {
                try
                {
                    await Client.StartWorkflowAsync(
                        workflowComponent: DaprWorkflowComponent,
                        workflowName: nameof(OrderProcessingWorkflow),
                        input: orderInfo,
                        instanceId: orderId);

                    GetWorkflowResponse state = await Client.WaitForWorkflowStartAsync(
                        instanceId: orderId,
                        workflowComponent: DaprWorkflowComponent);

                    Console.WriteLine("Your workflow has started. Here is the status of the workflow: {0}", state.RuntimeStatus);

                    state = await Client.WaitForWorkflowCompletionAsync(
                        instanceId: orderId,
                        workflowComponent: DaprWorkflowComponent);

                    Console.WriteLine("Workflow Status: {0}", state.RuntimeStatus);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Caught {0}", ex.ToString());
                    ExecutionFailureCount.Inc();
                }
            }
            Counter++;
            if (Counter > 4)
            {
                Counter = 0;
            }
        }
    }
}