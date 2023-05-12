/*
Copyright 2021 The Dapr Authors
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
    http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using System;

namespace PubsubWorkflow.Services
{
    public class MessagePublishingService
    {
        private readonly DaprClient Client;

        public MessagePublishingService([FromServices] DaprClient client)
        {
            Client = client;
        }

        internal async void Publish(object stateInfo)
        {
            var request = stateInfo as PublishRequest;
            await Client.PublishEventAsync(request.PubsubName, request.Topic, Guid.NewGuid().ToString());
        }
    }

    public record PublishRequest
    {
        public string PubsubName { get; set; }

        public string Topic { get; set; }
    }
}