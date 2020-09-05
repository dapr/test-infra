// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
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