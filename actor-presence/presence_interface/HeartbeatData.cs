// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Tests.Actors.PresenceTest {

    using System;
    using System.Linq;
    using System.Text;

    public class HeartbeatData
    {
        public Guid Game { get; set; }

        public GameStatus Status { get; private set; }

        public HeartbeatData()
        {
            Status = new GameStatus();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Heartbeat:");
            sb.Append(",Game=").Append(Game);
            var playerList = Status.Players.ToArray();
            for (int i = 0; i < playerList.Length; i++)
            {
                sb.AppendFormat(",Player{0}=", i + 1).Append(playerList[i]);
            }
            sb.AppendFormat(",Score={0}", Status.Score);
            return sb.ToString();
        }
    }
}