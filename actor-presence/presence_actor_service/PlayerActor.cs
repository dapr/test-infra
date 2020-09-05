// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Dapr.Tests.Actors.PresenceTest
{
    using System.Threading.Tasks;
    using Dapr.Actors;
    using Dapr.Actors.Runtime;

    class PlayerActor : Actor, IPlayerActor
    {                                            
        private const string StateName = "state";

        public PlayerActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        protected override async Task OnActivateAsync()
        {
            await this.StateManager.TryAddStateAsync(StateName, new PlayerState());
            await base.OnActivateAsync();
        } 

        public async Task<IGameActor> GetCurrentGame()
        {
            return (await this.StateManager.GetStateAsync<PlayerState>(StateName)).CurrentGame;
        }

        public async Task JoinGame(IGameActor game)
        {
            var state = await this.StateManager.GetStateAsync<PlayerState>(StateName);
            state.CurrentGame = game;
            await this.StateManager.SetStateAsync(StateName, state);
        }

        public async Task LeaveGame(IGameActor nullgame)
        {
            var state = await this.StateManager.GetStateAsync<PlayerState>(StateName);
            state.CurrentGame = null;
            await this.StateManager.SetStateAsync<PlayerState>(StateName, state);
        }
    }
}