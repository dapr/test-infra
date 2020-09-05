// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Dapr.Tests.Actors.PresenceTest
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Threading.Tasks;

    using Dapr.Actors;
    using Dapr.Actors.Client;
    using Dapr.Actors.Runtime;

    class GameActor : Actor, IGameActor
    {
        private const string StateName = "state";

        private byte[] ipAddressBytes;

        public GameActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        protected override async Task OnActivateAsync()
        {
            try
            {
                await this.StateManager.TryAddStateAsync(StateName, new GameState
                {
                    Players = new HashSet<Guid>(),
                    Status = new GameStatus {Score = new byte[1]}
                });
                await base.OnActivateAsync();
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Exception in OnActivateAsync: {e}");
                throw;
            }
        }

        public async Task<string> Initialize(int scoreSizeInBytes)
        {
            try
            {
                var state = await this.StateManager.GetStateAsync<GameState>(StateName);
                state.Status.Score = new byte[scoreSizeInBytes];
                Random r = new Random();
                r.NextBytes(state.Status.Score);
                await this.StateManager.SetStateAsync(StateName, state);

                return "";
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Exception in InitializeScoreBuffer: {e}");
                throw;
            }
        }

        public async Task UpdateGameStatus(GameStatus newStatus)
        {
            try
            {
                IPAddress senderIPAddress;
                int senderProcessId;
                long requestId;
                var scoreBuffer = newStatus.Score;

                if (scoreBuffer[0] == (byte)ScoreBufferContentType.NoRequestId)
                {
                    return;
                }

                var ipAddressLength = BitConverter.ToInt32(scoreBuffer, 1);
                GetIPAddressBytes(scoreBuffer, 1+sizeof(int), ipAddressLength);
                senderIPAddress = new IPAddress(this.ipAddressBytes);
                senderProcessId = BitConverter.ToInt32(scoreBuffer, 1 + sizeof(int) + ipAddressLength);
                requestId = BitConverter.ToInt64(scoreBuffer, 1 + (2*sizeof(int)) + ipAddressLength);

                try
                {
                    var state = await this.StateManager.GetStateAsync<GameState>(StateName);
                    state.Status = newStatus;

                    // Check for new players that joined since last update
                    foreach (var player in newStatus.Players)
                    {
                        if (!state.Players.Contains(player))
                        {
                            try
                            {
                                // Here we call player grains serially, which is less efficient than a fan-out but simpler to express.
                                await ActorProxy.Create<IPlayerActor>(new ActorId(player.ToString()), "PlayerActor").JoinGame(this);
                                state.Players.Add(player);
                            }
                            catch
                            {
                                // Ignore exceptions while telling player grains to join the game. 
                                // Since we didn't add the player to the list, this will be tried again with next update.
                            }
                        }
                    }

                    // Check for players that left the game since last update
                    var promises = new List<Task>();
                    foreach (var player in state.Players)
                    {
                        if (!newStatus.Players.Contains(player))
                        {
                            try
                            {
                                // Here we do a fan-out with multiple calls going out in parallel. We join the promisses later.
                                // More code to write but we get lower latency when calling multiple player grains.
                                promises.Add(ActorProxy.Create<IPlayerActor>(new ActorId(player.ToString()), "PlayerActor").LeaveGame(this));
                                state.Players.Remove(player);
                            }
                            catch
                            {
                                // Ignore exceptions while telling player grains to leave the game.
                                // Since we didn't remove the player from the list, this will be tried again with next update.
                            }
                        }
                    }

                    // Joining promises
                    await Task.WhenAll(promises);

                    await this.StateManager.SetStateAsync(StateName, state);
                }
                finally
                {
                    System.Console.WriteLine($"Processed request. {requestId},{senderProcessId},{senderIPAddress}");
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Exception in UpdateGameStatus: {e}");
                throw;
            }
        }

        public async Task<byte[]> GetGameScore()
        {
            try
            {
                return (await this.StateManager.GetStateAsync<GameState>(StateName)).Status.Score;
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Exception in GetGameScore: {e}");
                throw;
            }
        }

        private void GetIPAddressBytes(byte[] scoreBuffer, int startIndex, int length)
        {
            if ((null == this.ipAddressBytes) || (this.ipAddressBytes.Length != length))
            {
                this.ipAddressBytes = new byte[length];
            }
            Array.Copy(scoreBuffer, startIndex, this.ipAddressBytes, 0, length);
        }
    }
}