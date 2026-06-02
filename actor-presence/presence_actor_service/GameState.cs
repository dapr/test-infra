// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.Actors.PresenceTest
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    class GameState
    {
        public GameStatus Status;

        public HashSet<Guid> Players;
    }
}