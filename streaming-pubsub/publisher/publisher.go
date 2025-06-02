/*
Copyright 2021 The Dapr Authors
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
    http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

package main

import (
	"context"
	"fmt"
	"log"
	"math/rand"
	"os"
	"os/signal"
	"syscall"
	"time"

	logger "test-infra/streaming-pubsub/common"

	dapr "github.com/dapr/go-sdk/client"
)

var (
	// set the environment as instructions.
	pubSubName = "longhaul-streaming-pubsub"
	topicName1 = "orders"
	topicName2 = "shipments"
	throttler  = logger.NewThrottledLogger(10 * time.Minute)
)

func publishMessages(ctx context.Context, client dapr.Client) error {
	// Create event data with timestamp for tracking
	singleEventData := map[string]interface{}{
		"message":   "ping123",
		"id":        fmt.Sprintf("%d", time.Now().UnixNano()),
		"timestamp": time.Now().UTC(),
	}

	multiEventData := []interface{}{
		map[string]interface{}{
			"message":   "multi-ping",
			"id":        fmt.Sprintf("%d", time.Now().UnixNano()),
			"timestamp": time.Now().UTC(),
		},
		map[string]interface{}{
			"message":   "multi-pong",
			"id":        fmt.Sprintf("%d", time.Now().UnixNano()),
			"timestamp": time.Now().UTC(),
		},
	}

	// Publish single event on two different topics
	if err := client.PublishEvent(ctx, pubSubName, topicName1, singleEventData); err != nil {
		return fmt.Errorf("error publishing event to topic1: %v", err)
	}
	if err := client.PublishEvent(ctx, pubSubName, topicName2, singleEventData); err != nil {
		return fmt.Errorf("error publishing event to topic2: %v", err)
	}
	if throttler.ShouldLog("singleEvent") {
		log.Printf("Published events on topics %s and %s at %s\n", topicName1, topicName2, time.Now().UTC())
	}

	// Publish multiple events (bulk publish) on two topics
	if res := client.PublishEvents(ctx, pubSubName, topicName1, multiEventData); res.Error != nil {
		return fmt.Errorf("error publishing events to topic1: %v", res.Error)
	}
	if res := client.PublishEvents(ctx, pubSubName, topicName2, multiEventData); res.Error != nil {
		return fmt.Errorf("error publishing events to topic2: %v", res.Error)
	}
	if throttler.ShouldLog("multiEvent") {
		log.Printf("Bulk-published events on topics %s and %s at %s\n", topicName1, topicName2, time.Now().UTC())
	}

	return nil
}

func main() {
	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	client, err := dapr.NewClient()
	if err != nil {
		log.Fatalf("error creating dapr client: %v", err)
	}
	defer client.Close()

	sigCh := make(chan os.Signal, 1)
	signal.Notify(sigCh, os.Interrupt, syscall.SIGTERM)

	rnd := rand.New(rand.NewSource(time.Now().UnixNano()))

	go func() {
		for {
			select {
			case <-ctx.Done():
				return
			default:
				if err := publishMessages(ctx, client); err != nil {
					log.Printf("Error publishing messages: %v", err)
				}

				sleepDuration := time.Duration(1+rnd.Intn(10)) * time.Second
				time.Sleep(sleepDuration)
			}
		}
	}()

	<-sigCh
	log.Println("Received shutdown signal, stopping publisher...")
	cancel()
	time.Sleep(time.Second)
}
