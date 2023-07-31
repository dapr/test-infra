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
        IConfiguration _configuration;

        public ProcessPaymentActivity(ILoggerFactory loggerFactory, DaprClient client, IConfiguration configuration)
        {
            this.logger = loggerFactory.CreateLogger<ProcessPaymentActivity>();
            this.client = client;
            this._configuration = configuration;
        }

        public override async Task<object> RunAsync(WorkflowActivityContext context, PaymentRequest req)
        {
            this.logger.LogInformation(
                "Processing payment: {requestId} for {amount} {item} at ${currency}",
                req.RequestId,
                req.Amount,
                req.ItemBeingPruchased,
                req.Currency);

            await Task.Delay(TimeSpan.FromSeconds(Convert.ToDouble(this._configuration.GetValue<string>("PaymentProcessingTime"))));

            this.logger.LogInformation(
                "Payment for request ID '{requestId}' processed successfully",
                req.RequestId);

            return null;
        }
    }
}
