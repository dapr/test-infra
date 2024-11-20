package api

import (
	"context"
	"fmt"

	"github.com/dapr/go-sdk/actor"
	dapr "github.com/dapr/go-sdk/client"
	"github.com/dapr/go-sdk/examples/actor/api"
)

const playerActorType = "playerActorType"

type PlayerActor struct {
	actor.ServerImplBaseCtx
	DaprClient dapr.Client
	Health     int
}

func (p *PlayerActor) Type() string {
	return playerActorType
}

type GetPlayerRequest struct {
	ActorID string
}

type GetPlayerResponse struct {
	ActorID string
	Health  int
}

// GetUser retrieving the state of the PlayerActor
func (p *PlayerActor) GetUser(ctx context.Context, player *GetPlayerRequest) (*GetPlayerResponse, error) {
	if player.ActorID == p.ID() {
		fmt.Printf("Player Actor ID: %s has a health level of: %d\n", p.ID(), p.Health)
		return &GetPlayerResponse{
			ActorID: p.ID(),
			Health:  p.Health,
		}, nil
	}
	return nil, nil
}

// Invoke invokes an action on the actor
func (p *PlayerActor) Invoke(ctx context.Context, req string) (string, error) {
	fmt.Println("get req = ", req)
	return req, nil
}

// RevivePlayer revives the actor players health back to 100
func (p *PlayerActor) RevivePlayer(ctx context.Context, id string) error {
	if id == p.ID() {
		fmt.Printf("Reviving player: %s\n", id)
		p.Health = 100
	}

	return nil
}

// ReminderCall executes logic to handle what happens when the reminder is triggered
// Dapr automatically calls this method when a reminder fires for the player actor
func (p *PlayerActor) ReminderCall(reminderName string, state []byte, dueTime string, period string) {
	fmt.Println("receive reminder = ", reminderName, " state = ", string(state), "duetime = ", dueTime, "period = ", period)
	if reminderName == "healthReminder" {
		// Increase health if below 100
		if p.Health < 100 {
			p.Health += 10
			if p.Health > 100 {
				p.Health = 100
			}
			fmt.Printf("Player Actor health increased. Current health: %d\n", p.Health)
		}
	} else if reminderName == "healthDecayReminder" {
		// Decrease health
		p.Health -= 5
		if p.Health < 0 {
			fmt.Println("Player Actor died...")
		}
		fmt.Printf("Health decreased. Current health: %d\n", p.Health)
	}

}

// StartReminder registers a reminder for the actor
func (p *PlayerActor) StartReminder(ctx context.Context, req *api.ReminderRequest) error {
	fmt.Println("Starting reminder:", req.ReminderName)
	return p.DaprClient.RegisterActorReminder(ctx, &dapr.RegisterActorReminderRequest{
		ActorType: p.Type(),
		ActorID:   p.ID(),
		Name:      req.ReminderName,
		DueTime:   req.Duration,
		Period:    req.Period,
		Data:      []byte(req.Data),
	})
}
