package api

import (
	"context"
	"fmt"

	"github.com/dapr/go-sdk/actor"
	dapr "github.com/dapr/go-sdk/client"
)

const playerActorType = "playerActorType"

type ClientStub struct {
	GetUser       func(ctx context.Context) (*GetPlayerResponse, error)
	Invoke        func(context.Context, string) (string, error)
	RevivePlayer  func(context.Context, string) error
	StartReminder func(context.Context, *ReminderRequest) error
	StopReminder  func(context.Context, *ReminderRequest) error
	ReminderCall  func(string, []byte, string, string) error
}

func (a *ClientStub) Type() string {
	return playerActorType
}

func (a *ClientStub) ID() string {
	return "player-1"
}

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

type ReminderRequest struct {
	ReminderName string `json:"reminder_name"`
	DueTime      string `json:"duration"`
	Period       string `json:"period"`
	Data         string `json:"data"`
}

// GetUser retrieving the state of the PlayerActor
func (p *PlayerActor) GetUser(ctx context.Context) (*GetPlayerResponse, error) {
	fmt.Printf("Player Actor ID: %s has a health level of: %d\n", p.ID(), p.Health)
	return &GetPlayerResponse{
		ActorID: p.ID(),
		Health:  p.Health,
	}, nil
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

// StartReminder registers a reminder for the actor
func (p *PlayerActor) StartReminder(ctx context.Context, req *ReminderRequest) error {
	fmt.Println("Starting reminder:", req.ReminderName)
	return p.DaprClient.RegisterActorReminder(ctx, &dapr.RegisterActorReminderRequest{
		ActorType: p.Type(),
		ActorID:   p.ID(),
		Name:      req.ReminderName,
		DueTime:   req.DueTime,
		Period:    req.Period,
		Data:      []byte(req.Data),
	})
}

// StopReminder unregisters a reminder for the actor
func (p *PlayerActor) StopReminder(ctx context.Context, req *ReminderRequest) error {
	fmt.Println("Stopping reminder:", req.ReminderName)
	return p.DaprClient.RegisterActorReminder(ctx, &dapr.RegisterActorReminderRequest{
		ActorType: p.Type(),
		ActorID:   p.ID(),
		Name:      req.ReminderName,
	})
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
