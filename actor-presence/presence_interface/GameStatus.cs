// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.Actors.PresenceTest
{
    using System;
    using System.Collections.Generic;

    public enum ScoreBufferContentType : byte
    {
        NoRequestId,
        HasRequestId
    }

    public class GameStatus
    {
        public HashSet<Guid> Players { get; private set; }

        public byte[] Score { get; set; }

        public GameStatus()
        {
            Players = new HashSet<Guid>();
        }
    }
}