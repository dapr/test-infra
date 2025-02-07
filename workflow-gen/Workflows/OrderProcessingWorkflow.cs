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

namespace WorkflowGen.Workflows;

using System.Threading.Tasks;
using Dapr.Workflow;
using DurableTask.Core.Exceptions;
using WorkflowGen.Activities;
using WorkflowGen.Models;

internal sealed class OrderProcessingWorkflow : Workflow<OrderPayload, OrderResult>
{
    public override async Task<OrderResult> RunAsync(WorkflowContext context, OrderPayload order)
    {
        var orderId = context.InstanceId;

        await context.CallActivityAsync(
            nameof(NotifyActivity),
            new Notification($"Received order {orderId} for {order.Quantity} {order.Name} at ${order.TotalCost}"));

        var result = await context.CallActivityAsync<InventoryResult>(
            nameof(ReserveInventoryActivity),
            new InventoryRequest(RequestId: orderId, order.Name, order.Quantity));
            
        if (!result.Success)
        {
            await context.CallActivityAsync(
                nameof(NotifyActivity),
                new Notification($"Insufficient inventory for {order.Name}"));
            return new OrderResult(Processed: false);
        }

        await context.CallActivityAsync(
            nameof(ProcessPaymentActivity),
            new PaymentRequest(RequestId: orderId, order.Name, order.Quantity, order.TotalCost));

        try
        {
            await context.CallActivityAsync(
                nameof(UpdateInventoryActivity),
                new PaymentRequest(RequestId: orderId, order.Name, order.Quantity, order.TotalCost));                
        }
        catch (TaskFailedException)
        {
            await context.CallActivityAsync(
                nameof(NotifyActivity),
                new Notification($"Order {orderId} Failed! You are now getting a refund"));
            return new OrderResult(Processed: false);
        }

        await context.CallActivityAsync(
            nameof(NotifyActivity),
            new Notification($"Order {orderId} has completed!"));

        return new OrderResult(Processed: true);
    }
}