// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.Actors.PresenceTest
{
    using System.Threading.Tasks;
    using System.Text.Json;

    using Dapr.Actors;
    using Dapr.Actors.Client;
    using Dapr.Actors.Runtime;

    class PresenceActor : Actor, IPresenceActor
    {
        public PresenceActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        public Task Heartbeat(byte[] data)
        {
            var heartbeatData = JsonSerializer.Deserialize<HeartbeatData>(data);
            var game = ActorProxy.Create<IGameActor>(new ActorId(heartbeatData.Game.ToString()), "GameActor");
            return game.UpdateGameStatus(heartbeatData.Status);
        }
    }
}