package main

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"os"
	"os/signal"
	"syscall"
	"time"

	"test-infra/scheduler-actor-reminders/api"

	dapr "github.com/dapr/go-sdk/client"
)

func main() {
	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	client, err := dapr.NewClient()
	if err != nil {
		panic(err)
	}
	defer client.Close()

	// Actor ID for the player 'session'
	actorID := "player-1"
	deathSignal := make(chan bool)

	// Start monitoring actor player's health
	go monitorPlayerHealth(ctx, client, actorID, deathSignal)

	// Start player actor health increase reminder
	err = client.RegisterActorReminder(ctx, &dapr.RegisterActorReminderRequest{
		ActorType: "playerActorType",
		ActorID:   actorID,
		Name:      "healthReminder",
		DueTime:   "10s",
		Period:    "20s", // Every 20 seconds, increase health
		Data:      []byte(`"Health increase reminder"`),
	})
	if err != nil {
		log.Fatalf("error starting health increase reminder: %v", err)
	}
	fmt.Println("Started healthReminder for actor:", actorID)

	// Start player actor  health decay reminder
	err = client.RegisterActorReminder(ctx, &dapr.RegisterActorReminderRequest{
		ActorType: "playerActorType",
		ActorID:   actorID,
		Name:      "healthDecayReminder",
		DueTime:   "0s",
		Period:    "2s", // Every 2 seconds, decay health
		Data:      []byte(`"Health decay reminder"`),
	})
	if err != nil {
		log.Fatalf("failed to start health decay reminder: %w", err)
	}

	go func() {
		for {
			select {
			case <-ctx.Done():
				return
			case <-deathSignal:
				fmt.Println("Player is dead. Unregistering reminders...")

				fmt.Println("Unregistering health increase reminder for actor...")
				err = client.UnregisterActorReminder(ctx, &dapr.UnregisterActorReminderRequest{
					ActorType: "playerActorType",
					ActorID:   actorID,
					Name:      "healthReminder",
				})
				if err != nil {
					log.Fatalf("error unregistering actor reminder: %v", err)
				}

				fmt.Println("Unregistering health decay reminder for actor...")
				err = client.UnregisterActorReminder(ctx, &dapr.UnregisterActorReminderRequest{
					ActorType: "playerActorType",
					ActorID:   actorID,
					Name:      "healthDecayReminder",
				})
				if err != nil {
					log.Fatalf("error unregistering actor reminder: %v", err)
				}

				fmt.Println("Player reminders unregistered. Reviving player...")
				req := &dapr.InvokeActorRequest{
					ActorType: "playerActorType",
					ActorID:   actorID,
					Method:    "RevivePlayer",
					Data:      []byte(`"player-1"`),
				}
				_, err = client.InvokeActor(ctx, req)
				if err != nil {
					log.Fatalf("error invoking actor method RevivePlayer: %v", err)
				}
				fmt.Println("Player revived, health reset to 100. Restarting reminders...")

				// Restart reminders
				err = client.RegisterActorReminder(ctx, &dapr.RegisterActorReminderRequest{
					ActorType: "playerActorType",
					ActorID:   actorID,
					Name:      "healthReminder",
					DueTime:   "10s",
					Period:    "20s",
					Data:      []byte(`"Health increase reminder"`),
				})
				if err != nil {
					log.Fatalf("error starting actor reminder: %v", err)
				}
				fmt.Println("Started health increase reminder for actor:", actorID)

				err = client.RegisterActorReminder(ctx, &dapr.RegisterActorReminderRequest{
					ActorType: "playerActorType",
					ActorID:   actorID,
					Name:      "healthDecayReminder",
					DueTime:   "0s",
					Period:    "2s", // Every 5 seconds, decay health
					Data:      []byte(`"Health decay reminder"`),
				})
				if err != nil {
					log.Fatalf("error starting health decay reminder: %v", err)
				}
				fmt.Println("Started health decay reminder for actor:", actorID)
			}
		}
	}()

	// Graceful shutdown on Ctrl+C or SIGTERM (for Docker/K8s graceful shutdown)
	signalChan := make(chan os.Signal, 1)
	signal.Notify(signalChan, syscall.SIGINT, syscall.SIGTERM)
	<-signalChan
	fmt.Println("Shutting down...")
}

// monitorPlayerHealth continuously checks the player's health every 5 seconds
// and signals via a channel if the player is dead (health <= 0).
func monitorPlayerHealth(ctx context.Context, client dapr.Client, actorID string, deathSignal chan bool) {
	for {
		select {
		case <-ctx.Done():
			return
		default:
			// Check actor player's health
			getPlayerRequest := &api.GetPlayerRequest{ActorID: actorID}
			requestData, err := json.Marshal(getPlayerRequest)
			if err != nil {
				log.Fatalf("error marshaling request data: %v", err)
			}

			req := &dapr.InvokeActorRequest{
				ActorType: "playerActorType",
				ActorID:   actorID,
				Method:    "GetUser",
				Data:      requestData,
			}
			resp, err := client.InvokeActor(ctx, req)
			if err != nil {
				log.Fatalf("error invoking actor method GetUser: %v", err)
			}

			playerResp := &api.GetPlayerResponse{}
			err = json.Unmarshal(resp.Data, playerResp)
			if err != nil {
				log.Fatalf("error unmarshaling player state: %v", err)
			}
			fmt.Printf("Player health: %v\n", playerResp.Health)

			// If health is zero or below, signal player death
			if playerResp.Health <= 0 {
				deathSignal <- true
			} else {
				fmt.Printf("Player is alive with health: %d\n", playerResp.Health)
			}

			// Sleep for 5 seconds before checking health again
			time.Sleep(5 * time.Second)
		}
	}
}
