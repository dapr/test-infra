package main

import (
	"context"
	"log"

	"test-infra/scheduler-actor-reminders/api"

	"github.com/dapr/go-sdk/actor"
	dapr "github.com/dapr/go-sdk/client"
)

const stateKey = "health"

type PlayerActor struct {
	actor.ServerImplBaseCtx
	DaprClient dapr.Client
	Health     int
}

func (p *PlayerActor) Type() string {
	return api.PlayerActorType
}

// GetUser retrieving the state of the PlayerActor
func (p *PlayerActor) GetUser(ctx context.Context) (*api.GetPlayerResponse, error) {
	log.Printf("Player Actor ID: %s has a health level of: %d\n", p.ID(), p.Health)
	return &api.GetPlayerResponse{
		ActorID: p.ID(),
		Health:  p.Health,
	}, nil
}

// Invoke invokes an action on the actor
func (p *PlayerActor) Invoke(ctx context.Context, req string) (string, error) {
	log.Println("get req = ", req)
	return req, nil
}

// RevivePlayer revives the actor players health back to 100
func (p *PlayerActor) RevivePlayer(ctx context.Context, id string) error {
	log.Printf("Reviving player: %s\n", p.ID())
	p.Health = 100
	if err := p.GetStateManager().Set(ctx, stateKey, p.Health); err != nil {
		log.Printf("error saving state: %v", err)
	}

	return nil
}

// StartReminder registers a reminder for the actor
func (p *PlayerActor) StartReminder(ctx context.Context, req *api.ReminderRequest) error {
	log.Println("Starting reminder:", req.ReminderName)
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
func (p *PlayerActor) StopReminder(ctx context.Context, req *api.ReminderRequest) error {
	log.Println("Stopping reminder:", req.ReminderName)
	return p.DaprClient.UnregisterActorReminder(ctx, &dapr.UnregisterActorReminderRequest{
		ActorType: p.Type(),
		ActorID:   p.ID(),
		Name:      req.ReminderName,
	})
}

// ReminderCall executes logic to handle what happens when the reminder is triggered
// Dapr automatically calls this method when a reminder fires for the player actor
func (p *PlayerActor) ReminderCall(reminderName string, state []byte, dueTime string, period string) {
	log.Println("received reminder = ", reminderName, " state = ", string(state), "duetime = ", dueTime, "period = ", period)
	switch reminderName {
	case "healthReminder":
		if p.Health < 100 {
			p.Health += 10
			if p.Health > 100 {
				p.Health = 100
			}
			log.Printf("Player Actor health increased. Current health: %d\n", p.Health)
		}
	case "healthDecayReminder":
		p.Health -= 5
		if p.Health < 0 {
			log.Println("Player Actor died...")
		}
		log.Printf("Health decreased. Current health: %d\n", p.Health)
	default:
		log.Printf("Unknown reminder: %s\n", reminderName)
		return
	}

	// Update the state of the actor
	err := p.GetStateManager().Set(context.TODO(), stateKey, p.Health)
	if err != nil {
		log.Printf("error saving state: %v", err)
	}
}
