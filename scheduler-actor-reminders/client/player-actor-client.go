package main

import (
	"context"
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
	if err := client.Wait(ctx, 15*time.Second); err != nil {
		log.Fatalf("error waiting for Dapr initialization: %v", err)
	}

	// Implement the actor client stub
	myActor := &api.ClientStub{
		ActorID: "player-1",
	}
	client.ImplActorClientStub(myActor)

	// Start monitoring actor player's health
	go monitorPlayerHealth(ctx, myActor)

	// Start player actor health increase reminder
	err = myActor.StartReminder(ctx, &api.ReminderRequest{
		ReminderName: "healthReminder",
		Period:       "20s",
		DueTime:      "10s",
		Data:         `"Health increase reminder"`,
	})
	if err != nil {
		// The first reminder registrations have to succeed,
		// if they don't, the app is not testing what we need to test and we need to exit
		log.Fatalf("error starting health increase reminder: %v", err)
	}
	defer stopReminder(ctx, myActor, "healthReminder")
	log.Println("Started healthReminder for actor:", myActor.ID())

	// Start player actor  health decay reminder
	err = myActor.StartReminder(ctx, &api.ReminderRequest{
		ReminderName: "healthDecayReminder",
		Period:       "2s", // Every 2 seconds, decay health
		DueTime:      "0s",
		Data:         `"Health decay reminder"`,
	})
	if err != nil {
		// The first reminder registrations have to succeed,
		// if they don't, the app is not testing what we need to test and we need to exit
		log.Fatalf("failed to start health decay reminder: %w", err)
	}
	defer stopReminder(ctx, myActor, "healthDecayReminder")
	log.Println("Started healthDecayReminder for actor:", myActor.ID())

	// Graceful shutdown on Ctrl+C or SIGTERM (for Docker/K8s graceful shutdown)
	signalChan := make(chan os.Signal, 1)
	signal.Notify(signalChan, syscall.SIGINT, syscall.SIGTERM)
	<-signalChan
	log.Println("Shutting down...")
}

// monitorPlayerHealth continuously checks the player's health every 5 seconds
// and signals via a channel if the player is dead (health <= 0).
func monitorPlayerHealth(ctx context.Context, actor *api.ClientStub) {
	for {
		select {
		case <-ctx.Done():
			return
		default:
			// Check actor player's health
			playerResp, err := actor.GetUser(ctx)
			if err != nil {
				log.Printf("error invoking actor method GetUser: %v", err)
				continue
			}

			log.Printf("Player health: %v\n", playerResp.Health)

			// If health is zero or below, signal player death
			if playerResp.Health <= 0 {
				revivePlayer(ctx, actor)
			} else {
				log.Printf("Player is alive with health: %d\n", playerResp.Health)
			}

			// Sleep for 5 seconds before checking health again
			time.Sleep(5 * time.Second)
		}
	}
}

func revivePlayer(ctx context.Context, actor *api.ClientStub) {
	log.Println("Player is dead. Unregistering reminders...")

	stopReminder(ctx, actor, "healthReminder")
	stopReminder(ctx, actor, "healthDecayReminder")

	log.Println("Player reminders unregistered. Reviving player...")
	err := actor.RevivePlayer(ctx, "player-1")
	if err != nil {
		log.Printf("error invoking actor method RevivePlayer: %v", err)
	}
	log.Println("Player revived, health reset to 100. Restarting reminders...")

	// Restart reminders
	err = actor.StartReminder(ctx, &api.ReminderRequest{
		ReminderName: "healthReminder",
		Period:       "20s",
		DueTime:      "10s",
		Data:         `"Health increase reminder"`,
	})
	if err != nil {
		log.Printf("error starting actor reminder: %v", err)
	}
	log.Println("Started health increase reminder for actor:", actor.ID())

	err = actor.StartReminder(ctx, &api.ReminderRequest{
		ReminderName: "healthDecayReminder",
		Period:       "2s",
		DueTime:      "0s",
		Data:         `"Health decay reminder"`,
	})
	if err != nil {
		log.Printf("error starting health decay reminder: %v", err)
	}
	log.Println("Started health decay reminder for actor:", actor.ID())
}

func stopReminder(ctx context.Context, myActor *api.ClientStub, reminderName string) {
	log.Printf("Unregistering '%s' reminder for actor...", reminderName)
	err := myActor.StopReminder(ctx, &api.ReminderRequest{
		ReminderName: reminderName,
	})
	if err != nil {
		log.Printf("error unregistering actor reminder '%s': %v", reminderName, err)
	}
}
