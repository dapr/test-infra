using System.Threading.Tasks;
using Dapr.Client;
using Dapr.Workflow;
using WorkflowGen.Models;
using Microsoft.Extensions.Logging;
using System;

namespace WorkflowGen.Activities
{

    class UpdateInventoryActivity : WorkflowActivity<PaymentRequest, Object>
    {
        static readonly string storeName = "statestore";
        readonly ILogger logger;
        readonly DaprClient client;

        public UpdateInventoryActivity(ILoggerFactory loggerFactory, DaprClient client)
        {
            this.logger = loggerFactory.CreateLogger<UpdateInventoryActivity>();
            this.client = client;
        }

        public override async Task<Object> RunAsync(WorkflowActivityContext context, PaymentRequest req)
        {
            this.logger.LogInformation(
                "Checking Inventory for: Order# {RequestId} for {Amount} {Item}",
                req.RequestId,
                req.Amount,
                req.ItemBeingPruchased);

            await Task.Delay(TimeSpan.FromSeconds(5));

            var original = await client.GetStateAsync<OrderPayload>(storeName, req.ItemBeingPruchased);
            int newQuantity = original.Quantity - req.Amount;

            if (newQuantity < 0)
            {
                this.logger.LogInformation(
                    "Payment for request ID '{RequestId}' could not be processed. Insufficient inventory.",
                    req.RequestId);
                throw new InvalidOperationException();
            }

            await client.SaveStateAsync<OrderPayload>(storeName, req.ItemBeingPruchased, new OrderPayload(Name: req.ItemBeingPruchased, TotalCost: req.Currency, Quantity: newQuantity));
            this.logger.LogInformation("There are now: {NewQuantity} {OriginalName} left in stock", newQuantity, original.Name);

            return null;
        }
    }
}
