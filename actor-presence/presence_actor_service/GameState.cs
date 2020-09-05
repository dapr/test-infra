// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
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