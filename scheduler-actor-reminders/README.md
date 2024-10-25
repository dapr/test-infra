# Scheduler Actor Reminders

## Overview
This project tests the Dapr Scheduler for handling Actor Reminders using an example `PlayerActor` actor. The example
simulates a game session where a player's health increases or decays periodically, managed through Dapr Actor reminders.

## Project Structure
The `client` directory implements the client code that interacts with `PlayerActor`, specifically setting up reminders,
monitoring health, and handling shutdown. It invokes actor methods, but does not manage the actor lifecycle.

The `server` directory implements the `PlayerActor` server code, which manages a game session for a player, including
health-based reminders. This code defines the actor lifecycle and its reminder-based state changes.

## Reminders
Two reminders manage the actor's health:
- `healthReminder`:
    - Increases the player's health if it is below full health.
- `healthDecayReminder`:
    - Decreases the player's health periodically, simulating a natural decay over time.

When the player's health reaches 0, the client unregisters the reminders, revives the player, and restarts the reminders.

This tests the Scheduler for the underlying storage for Actor Reminders.

## How To Run the Code:
Run the server with:
```shell
dapr run --app-id player-actor --app-port 8383 --dapr-http-port 3500 --log-level debug  --config ../dapr/config.yaml -- go run player-actor.go
```

Run the client with:
```shell
dapr run --app-id player-actor --app-port 50051 --dapr-http-port 3501 --dapr-grpc-port 50001 --log-level debug --config ../dapr/config.yaml -- go run player-actor-client.go
```

Note the config is using `SchedulerReminders`

Or

Build app images from `scheduler-actor-reminders` directory:
```shell
docker build -t player-actor-server -f Dockerfile-server .
docker build -t player-actor-client -f Dockerfile-client .
```

Run app containers:
```shell
# optionally add -d to both commands to run in background
docker run --name player-actor-server -p 8383:8383 player-actor-server
docker run --name player-actor-client -p 50051:50051 player-actor-client
```