// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

using Dapr.Client;
using Dapr.Tests.Common.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BindingWorkflow
{
    [ApiController]
    public class BindingsController : ControllerBase
    {
        /// <summary>
        /// Handle a message from the Azure Service Bus input binding.
        /// </summary>
        [HttpPost("/longhaul-invoke-binding")]
        public async Task<IActionResult> HandleBinding([FromBody] BindingInvocationMessage message, [FromServices] DaprClient client)
        {
            Console.WriteLine($"Invoking binding {message.TargetBinding} with operation \"{message.TargetOperation}\" {message.InvocationCount} times.");
            for (var i = 0; i < message.InvocationCount; i++)
            {
                var data = new Dictionary<string, string>(message.Data);
                if (message.Metadata.ContainsKey("autoGenId")) {
                    data.Add("id", Guid.NewGuid().ToString());
                }
                await client.InvokeBindingAsync(message.TargetBinding, message.TargetOperation, data);
            }
            Console.WriteLine("Finished invoking binding.");
            return Ok();
        }
    }
}