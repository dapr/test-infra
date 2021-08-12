// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PubsubWorkflow
{
    public class MessagePublisher
    {
        private readonly DaprClient Client;
        private readonly string PubsubName;
        private readonly string Topic;

        public MessagePublisher([FromServices] DaprClient client, string pubsubName, string topic)
        {
            this.Client = client;
            this.PubsubName = pubsubName;
            this.Topic = topic;
        }

        internal async void Publish(Object stateInfo)
        {
            await Client.PublishEventAsync(PubsubName, Topic, "Sample Message");
        }
    }
}