# Scheduler Workflow

## Overview

The long-haul test workflow performs the following operations repeatedly:

1. Initialize and Register Workflows & Activities:
    - A worker is set up and registers the `TestWorkflow` and `TestActivity`, which will execute in each iteration of the
      long-haul test.

2. Start Workflow Iterations:
    - In each iteration, a new workflow instance is started with an incremental identifier (e.g., `longhaul-instance-0`,
      `longhaul-instance-1`, etc.).

3. Workflow Lifecycle Management:
    - The workflow is paused and then resumed, simulating real-world scenarios where workflows may need to be temporarily
      halted and restarted.
    - An external event, named `testEvent`, is raised, allowing the workflow to proceed to the next stage upon receiving
      the event.

4. Stage Tracking:
    - Within `TestActivity`, a `stage` variable is incremented to track the current step of the workflow. This allows us
      to observe the workflow's progress and simulate step-based processing. Once stage reaches `100`, it resets to `0`.

5. Workflow Completion & Cleanup:
    - The test monitors each workflow’s status, polling every 5s, and waits until the workflow completes or fails.
    - Upon completion (or failure), the workflow is:
        - Terminated to ensure it doesn’t continue running or consuming resources.
        - Purged to remove the instance from the Dapr state, maintaining a clean test environment over time.

6. Iteration Limit:
    - After `100` iterations, the workflow `instanceID` counter resets to `0` to avoid excessive resource buildup.

## How To Run the Code:

Run the server with:
```shell
dapr run --app-id scheduler-workflow --app-port 3009 --dapr-http-port 3502 --log-level debug  --config dapr/config.yaml  -- go run workflow.go
```