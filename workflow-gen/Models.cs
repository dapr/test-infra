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

namespace WorkflowGen.Models;

internal sealed record OrderPayload(string Name, double TotalCost, int Quantity = 1);
internal sealed record InventoryRequest(string RequestId, string ItemName, int Quantity);
internal sealed record InventoryResult(bool Success, OrderPayload? OrderPayload);
internal sealed record PaymentRequest(string RequestId, string ItemBeingPurchased, int Amount, double Currency);
internal sealed record OrderResult(bool Processed);
internal sealed record InventoryItem(string Name, double TotalCost, int Quantity);