using System.Threading.Tasks;
using Dapr.Client;
using Dapr.Workflow;
using Microsoft.Extensions.Logging;
using WorkflowGen.Models;
using System;
using Microsoft.Extensions.Configuration;

namespace WorkflowGen.Activities
{

    class ProcessPaymentActivity : WorkflowActivity<PaymentRequest, object>
    {
        readonly ILogger logger;
        readonly DaprClient client;
        IConfiguration appSettings;

        public ProcessPaymentActivity(ILoggerFactory loggerFactory, DaprClient client)
        {
            this.logger = loggerFactory.CreateLogger<ProcessPaymentActivity>();
            this.client = client;
            this.appSettings = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetParent(System.IO.Directory.GetCurrentDirectory().ToString()).ToString())
                .AddJsonFile($"appsettings.json", optional: true)
                .Build();
        }

        public override async Task<object> RunAsync(WorkflowActivityContext context, PaymentRequest req)
        {
            this.logger.LogInformation(
                "Processing payment: {requestId} for {amount} {item} at ${currency}",
                req.RequestId,
                req.Amount,
                req.ItemBeingPruchased,
                req.Currency);

            await Task.Delay(TimeSpan.FromSeconds(Convert.ToDouble(this.appSettings["PaymentProcessingTime"])));

            this.logger.LogInformation(
                "Payment for request ID '{requestId}' processed successfully",
                req.RequestId);

            return null;
        }
    }
}
