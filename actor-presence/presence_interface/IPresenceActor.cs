// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.Actors.PresenceTest
{
    using System.Threading.Tasks;
    using Dapr.Actors;

    public interface IPresenceActor : IActor
    {
        Task Heartbeat(byte[] data);
    }
}