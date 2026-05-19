// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.Actors.PresenceTest
{
    using System.Threading.Tasks;
    using Dapr.Actors;

    public interface IPlayerActor : IActor
    {
        Task<IGameActor> GetCurrentGame();

        Task JoinGame(IGameActor game);

        Task LeaveGame(IGameActor game);
    }
}