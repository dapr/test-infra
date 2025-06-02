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

using System.Threading.Tasks;
using Dapr.Workflow;
using Microsoft.Extensions.Logging;
using WorkflowGen.Models;
using System;
using Microsoft.Extensions.Configuration;

namespace WorkflowGen.Activities;

internal sealed partial class ProcessPaymentActivity(ILogger<ProcessPaymentActivity> logger, IConfiguration configuration) : WorkflowActivity<PaymentRequest, object?>
{
    public override async Task<object?> RunAsync(WorkflowActivityContext context, PaymentRequest req)
    {
        LogProcessingPayment(logger, req.RequestId, req.Amount, req.ItemBeingPurchased, req.Currency);

        await Task.Delay(TimeSpan.FromSeconds(Convert.ToDouble(configuration.GetValue<string>("PaymentProcessingTime"))));

        LogSuccessfulPayment(logger, req.RequestId);

        return null;
    }

    [LoggerMessage(LogLevel.Information, "Processing payment: {requestId} for {amount} {item} at ${currency}")]
    static partial void LogProcessingPayment(ILogger logger, string requestId, int amount, string item, double currency);

    [LoggerMessage(LogLevel.Information, "Payment for request ID '{requestId}' processed successfully")]
    static partial void LogSuccessfulPayment(ILogger logger, string requestId);
}