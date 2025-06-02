# Streaming subscriptions

## Overview

This is a pub/sub example demonstrating streaming message subscription using Dapr.

The publisher:
- Publishes single events to two topics (`orders` and `shipments`)
- Publishes bulk events (multiple messages) to both topics
- Runs indefinitely, publishing messages with random intervals (1-10 seconds)

The subscriber:
- Subscribes to both topics (`orders` and `shipments`)
- Uses throttled logging (every 10 seconds) to prevent log flooding
- Processes both single and bulk messages
- Runs indefinitely until interrupted

## How-To Run Locally:

Run the publisher:
```shell
dapr run \
  --app-id publisher \
  -- go run publisher/publisher.go
```

Run the streaming subscriber:
```shell
dapr run \
  --app-id subscriber \
  -- go run subscriber/subscriber.go
```