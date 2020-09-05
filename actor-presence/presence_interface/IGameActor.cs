// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
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