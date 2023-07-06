// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace WorkflowGen
{
    using Dapr.Client;
    using Dapr.Workflow;
    using Dapr.Tests.Common.Models;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Prometheus;
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using WorkflowGen.Activities;
    using WorkflowGen.Models;
    using WorkflowGen.Workflows;
    /// <summary>
    /// WorkflowGenerator - runs worflows and executes them using Dapr.
    /// The main functionality is in StartWorkflowGeneratorAsync().
    /// </summary>
    public class Program
    {
        private static readonly Gauge PublishCallTime = Metrics.CreateGauge("lh_workflow_generator_publish_call_time", "The time it takes for the workflow call to return");

        private static readonly Counter PublishFailureCount = Metrics.CreateCounter("lh_workflow_generator_publish_failure_count", "Publich calls that throw");

        const string DaprWorkflowComponent = "dapr";


        /// <summary>
        /// Main for FeedGenerator
        /// </summary>
        /// <param name="args">Arguments.</param>
        public static void Main(string[] args)
        {
            int delayInMilliseconds = 10000;

            var server = new MetricServer(port: 9988);
            server.Start();

            var builder = Host.CreateDefaultBuilder(args).ConfigureServices(services =>
            {
                services.AddDaprWorkflow(options =>
                {
                    // Note that it's also possible to register a lambda function as the workflow
                    // or activity implementation instead of a class.
                    options.RegisterWorkflow<OrderProcessingWorkflow>();

                    // These are the activities that get invoked by the workflow(s).
                    options.RegisterActivity<NotifyActivity>();
                    options.RegisterActivity<ReserveInventoryActivity>();
                    options.RegisterActivity<ProcessPaymentActivity>();
                    options.RegisterActivity<UpdateInventoryActivity>();
                });
            });

            Task.Run(() => StartWorkflowGeneratorAsync(delayInMilliseconds));

            using var host = builder.Build();
            host.Run();


        }

        static internal async void StartWorkflowGeneratorAsync(int delayInMilliseconds)
        {

            TimeSpan delay = TimeSpan.FromMilliseconds(delayInMilliseconds);

            DaprClientBuilder daprClientBuilder = new DaprClientBuilder();

            DaprClient client = daprClientBuilder.Build();
            var Counter = 0;
            await client.SaveStateAsync<OrderPayload>("statestore", "Cars",  new OrderPayload(Name: "cars", TotalCost: 15000, Quantity: 100));

            while (true)
            {
                Random random = new Random();
                var num = random.Next(35);
                OrderPayload orderInfo = new OrderPayload("Cars", num * 1000, num);
                string orderId = Guid.NewGuid().ToString()[..8];

                try
                {
                    Console.WriteLine("Publishing");
                    using (PublishCallTime.NewTimer())
                    {

                        await client.StartWorkflowAsync(
                            workflowComponent: DaprWorkflowComponent,
                            workflowName: nameof(OrderProcessingWorkflow),
                            input: orderInfo,
                            instanceId: orderId);

                            GetWorkflowResponse state = await client.WaitForWorkflowStartAsync(
                                instanceId: orderId,
                                workflowComponent: DaprWorkflowComponent);

                            Console.WriteLine("Your workflow has started. Here is the status of the workflow: {0}", state.RuntimeStatus);

                            state = await client.WaitForWorkflowCompletionAsync(
                                instanceId: orderId,
                                workflowComponent: DaprWorkflowComponent);

                            Console.WriteLine("Workflow Status: {0}", state.RuntimeStatus);

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Caught {0}", e.ToString());
                    PublishFailureCount.Inc();
                }

                Counter++;
                if (Counter > 5){
                    await client.SaveStateAsync<OrderPayload>("statestore", "Cars",  new OrderPayload(Name: "Cars", TotalCost: 15000, Quantity: 100));
                    Counter = 0;
                }
                await Task.Delay(delay);
            }
        }
    }
}
