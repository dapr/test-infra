using System.Threading.Tasks;
using Dapr.Client;
using Dapr.Workflow;
using Microsoft.Extensions.Logging;
using WorkflowGen.Models;
using System;

namespace WorkflowGen.Activities
{

    class ReserveInventoryActivity : WorkflowActivity<InventoryRequest, InventoryResult>
    {
        readonly ILogger logger;
        readonly DaprClient client;
        static readonly string storeName = "statestore";

        public ReserveInventoryActivity(ILoggerFactory loggerFactory, DaprClient client)
        {
            this.logger = loggerFactory.CreateLogger<ReserveInventoryActivity>();
            this.client = client;
        }

        public override async Task<InventoryResult> RunAsync(WorkflowActivityContext context, InventoryRequest req)
        {
            this.logger.LogInformation(
                "Reserving inventory for order {requestId} of {quantity} {name}",
                req.RequestId,
                req.Quantity,
                req.ItemName);


            var orderResponse = await client.GetStateAsync<OrderPayload>(storeName, req.ItemName);

            if (orderResponse == null)
            {
                return new InventoryResult(false, orderResponse);
            }

            this.logger.LogInformation(
                "There are: {requestId}, {name} available for purchase",
                orderResponse.Quantity,
                orderResponse.Name);

            if (orderResponse.Quantity >= req.Quantity)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));

                return new InventoryResult(true, orderResponse);
            }

            return new InventoryResult(false, orderResponse);

        }
    }
}
