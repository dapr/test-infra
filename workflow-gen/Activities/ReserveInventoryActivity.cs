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
using Microsoft.Extensions.Logging;
using WorkflowGen.Models;
using System;

namespace WorkflowGen.Activities;

internal sealed partial class ReserveInventoryActivity(ILogger<ReserveInventoryActivity> logger, DaprClient daprClient) : WorkflowActivity<InventoryRequest, InventoryResult>
{
    private const string StoreName = "statestore";

    public override async Task<InventoryResult> RunAsync(WorkflowActivityContext context, InventoryRequest req)
    {
        LogReservingInventory(logger, req.RequestId, req.Quantity, req.ItemName);

        var orderResponse = await daprClient.GetStateAsync<OrderPayload>(StoreName, req.ItemName);

        if (orderResponse is null)
        {
            return new InventoryResult(false, orderResponse);
        }

        LogAvailableInventory(logger, orderResponse.Quantity, orderResponse.Name);

        if (orderResponse.Quantity >= req.Quantity)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            return new InventoryResult(true, orderResponse);
        }

        return new InventoryResult(false, orderResponse);

    }

    [LoggerMessage(LogLevel.Information, "Reserving inventory for order {requestId} of {quantity} {name}")]
    static partial void LogReservingInventory(ILogger logger, string requestId, int quantity, string name);

    [LoggerMessage(LogLevel.Information, "There are: {quantity}, {name} available for purchase")]
    static partial void LogAvailableInventory(ILogger logger, int quantity, string name);
}