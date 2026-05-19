// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.Actors.PresenceTest
{
    using System;
    using System.Threading.Tasks;
    using Dapr.Actors;

    public interface IGameActor : IActor
    {
        Task<string> Initialize(int scoreSizeInBytes);

        Task UpdateGameStatus(GameStatus status);

        Task<byte[]> GetGameScore();
    }
}