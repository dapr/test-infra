# Scheduler Jobs

## Overview

- schedule 100 oneshot jobs indefinitely (repeat = 1)
- schedule 100 indefinite jobs indefinitely (repeat not set, trigger every 30s)
- schedule repeat-job job indefinitely (repeat = 5, trigger every 1s due immediately)
- indefinitely schedule and delete a create-delete-job job (repeat = 1, trigger every 1s)

## How-To Run Locally:

Run with dapr:
```shell
dapr run \
  --app-id scheduler-jobs \
  --app-port 8383 \
  --dapr-grpc-port 3501 --app-protocol grpc \
  --dapr-http-port 3500 --scheduler-host-address=127.0.0.1:50006 --app-channel-address=127.0.0.1 \
  -- go run scheduler-jobs.go
```

To run locally as a container:
```shell
docker build -t scheduler-jobs .
docker run -p 8383:8383 --name scheduler-jobs scheduler-jobs # optionally add -d to run in background
# check container is running
docker ps
```