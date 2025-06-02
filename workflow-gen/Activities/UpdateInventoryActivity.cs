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
using Dapr.Client;
using Dapr.Workflow;
using WorkflowGen.Models;
using Microsoft.Extensions.Logging;
using System;

namespace WorkflowGen.Activities;

internal sealed partial class UpdateInventoryActivity(ILogger<UpdateInventoryActivity> logger, DaprClient daprClient)  : WorkflowActivity<PaymentRequest, object?>
{
    private const string StoreName = "statestore";

    public override async Task<object?> RunAsync(WorkflowActivityContext context, PaymentRequest req)
    {
        LogCheckingInventory(logger, req.RequestId, req.Amount, req.ItemBeingPurchased);

        await Task.Delay(TimeSpan.FromSeconds(5));

        var original = await daprClient.GetStateAsync<OrderPayload>(StoreName, req.ItemBeingPurchased);
        var newQuantity = original.Quantity - req.Amount;

        if (newQuantity < 0)
        {
            LogInsufficientInventory(logger, req.RequestId);
            throw new InvalidOperationException();
        }

        await daprClient.SaveStateAsync<OrderPayload>(StoreName, req.ItemBeingPurchased, new OrderPayload(Name: req.ItemBeingPurchased, TotalCost: req.Currency, Quantity: newQuantity));
        LogRemainingStock(logger, newQuantity, original.Name);

        return null;
    }

    [LoggerMessage(LogLevel.Information, "Checking Inventory for: Order# {requestId} for {amount} {item}")]
    static partial void LogCheckingInventory(ILogger logger, string requestId, int amount, string item);

    [LoggerMessage(LogLevel.Information, "Payment for request ID '{requestId}' could not be processed. Insufficient inventory.")]
    static partial void LogInsufficientInventory(ILogger logger, string requestId);

    [LoggerMessage(LogLevel.Information, "There are now: {newQuantity} {originalName} left in stock")]
    static partial void LogRemainingStock(ILogger logger, int newQuantity, string originalName);
}