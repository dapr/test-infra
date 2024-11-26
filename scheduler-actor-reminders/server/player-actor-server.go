package main

import (
	"context"
	"log"
	http2 "net/http"
	"os"
	"os/signal"
	"syscall"

	"github.com/dapr/go-sdk/actor"
	dapr "github.com/dapr/go-sdk/client"
	"github.com/dapr/go-sdk/service/http"
)

const appPort = ":3007"

func playerActorFactory() actor.ServerContext {
	client, err := dapr.NewClient()
	if err != nil {
		panic(err)
	}

	return &PlayerActor{
		DaprClient: client,
		Health:     100, // initial health
	}
}

func main() {
	_, cancel := context.WithCancel(context.Background())
	defer cancel()

	daprService := http.NewService(appPort)
	// Register actor factory, meaning register actor methods to be called by client
	daprService.RegisterActorImplFactoryContext(playerActorFactory)

	go func() {
		log.Println("Starting Dapr actor runtime...")
		if err := daprService.Start(); err != nil && err.Error() != http2.ErrServerClosed.Error() {
			log.Fatalf("error starting Dapr actor runtime: %v", err)
		}
	}()

	waitForShutdown(cancel)
	if err := daprService.GracefulStop(); err != nil {
		log.Fatalf("error stopping Dapr actor runtime: %v", err)
	}
}

// waitForShutdown keeps the app alive until an interrupt or termination signal is received
func waitForShutdown(cancelFunc context.CancelFunc) {
	sigCh := make(chan os.Signal, 1)
	// Notify the channel on Interrupt (Ctrl+C) or SIGTERM (for Docker/K8s graceful shutdown)
	signal.Notify(sigCh, os.Interrupt, syscall.SIGTERM)
	<-sigCh

	log.Println("Shutting down...")
	cancelFunc()
}
