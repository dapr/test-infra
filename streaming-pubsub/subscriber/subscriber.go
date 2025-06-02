/*
Copyright 2024 The Dapr Authors
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
    http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES
*/

package main

import (
	"context"
	"log"
	"os"
	"os/signal"
	"syscall"
	"time"

	logger "test-infra/streaming-pubsub/common"

	daprd "github.com/dapr/go-sdk/client"
	"github.com/dapr/go-sdk/service/common"
)

var (
	pubsubName     = "longhaul-streaming-pubsub"
	topicOrders    = "orders"
	topicShipments = "shipments"
	throttler      = logger.NewThrottledLogger(10 * time.Minute)
)

func main() {
	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	client, err := daprd.NewClient()
	if err != nil {
		log.Fatal(err)
	}
	defer client.Close()

	stopOrders, err := client.SubscribeWithHandler(ctx,
		daprd.SubscriptionOptions{
			PubsubName: pubsubName,
			Topic:      topicOrders,
		},
		handleOrder,
	)
	if err != nil {
		log.Fatalf("failed to subscribe to sendorder: %v", err)
	}
	defer stopOrders()
	log.Printf("Created subscription %s/%s\n", pubsubName, topicOrders)

	// Subscribe to neworder
	stopNewOrder, err := client.SubscribeWithHandler(ctx,
		daprd.SubscriptionOptions{
			PubsubName: pubsubName,
			Topic:      topicShipments,
		},
		handleShipment,
	)
	if err != nil {
		log.Fatalf("failed to subscribe to neworder: %v", err)
	}
	defer stopNewOrder()
	log.Printf("Created subscription %s/%s\n", pubsubName, topicShipments)

	// Wait for shutdown signal
	sigCh := make(chan os.Signal, 1)
	signal.Notify(sigCh, os.Interrupt, syscall.SIGTERM)
	<-sigCh

	log.Println("Shutting down subscriber...")
	cancel()
}

func handleOrder(e *common.TopicEvent) common.SubscriptionResponseStatus {
	if throttler.ShouldLog("handleOrder") {
		log.Printf("Received order: %s\n", e.Data)
	}
	return common.SubscriptionResponseStatusSuccess
}

func handleShipment(e *common.TopicEvent) common.SubscriptionResponseStatus {
	if throttler.ShouldLog("handleShipment") {
		log.Printf("Received shipment: %s\n", e.Data)
	}
	return common.SubscriptionResponseStatusSuccess
}
