package main

import (
	"context"
	"fmt"
	"log"
	"net"
	"os"
	"os/signal"
	"strconv"
	"strings"
	"sync/atomic"
	"syscall"
	"time"

	"google.golang.org/grpc"

	rtv1 "github.com/dapr/dapr/pkg/proto/runtime/v1"
	"github.com/dapr/go-sdk/client"
)

const appPort = ":3006"

var oneshot atomic.Int64
var indefinite atomic.Int64
var repeat atomic.Int64
var createDelete atomic.Int64

const maxOneshotTriggerCount = 100
const maxIndefiniteTriggerCount = 100
const maxRepeatTriggerCount = 5
const maxCreateDeleteTriggerCount = 1

// Channel to trigger scheduling new jobs after reaching the max count
var oneshotDoneCh = make(chan struct{}, 1)
var indefiniteDoneCh = make(chan struct{}, 1)
var repeatDoneCh = make(chan struct{}, 1)
var receivedSingleJobDoneCh = make(chan struct{}, 1)
var createDeleteDoneCh = make(chan struct{}, 1)

type appServer struct {
	client client.Client
}

// startServer starts a gRPC server for receiving callbacks
func startServer(ctx context.Context) error {
	lis, err := net.Listen("tcp", appPort)
	if err != nil {
		return fmt.Errorf("failed to listen on %v: %w", appPort, err)
	}

	s := grpc.NewServer()
	rtv1.RegisterAppCallbackAlphaServer(s, &appServer{})

	errCh := make(chan error, 1)

	go func() {
		log.Printf("Starting gRPC server on port %s...\n", appPort)
		if err := s.Serve(lis); err != nil {
			errCh <- fmt.Errorf("failed to serve: %w", err)
		}
	}()

	select {
	case <-ctx.Done():
		log.Println("Context canceled, shutting down gRPC server...")
		s.GracefulStop()
		return nil
	case err := <-errCh:
		return err
	}
}

func (s *appServer) OnBulkTopicEventAlpha1(ctx context.Context, in *rtv1.TopicEventBulkRequest) (*rtv1.TopicEventBulkResponse, error) {
	return nil, nil
}

func (s *appServer) OnJobEventAlpha1(ctx context.Context, in *rtv1.JobEventRequest) (*rtv1.JobEventResponse, error) {
	if strings.HasPrefix(in.GetMethod(), "job/") {
		if strings.Contains(in.GetMethod(), "oneshot") {
			count := oneshot.Add(1)
			if count == maxOneshotTriggerCount {
				// Reset the oneshot counter
				oneshot.Store(0)
				log.Println("Reached max oneshot job count, scheduling new jobs...")
				// Send signal to start scheduling another batch of one shot jobs
				oneshotDoneCh <- struct{}{}
			}
		} else if strings.Contains(in.GetMethod(), "indefinite") {
			count := indefinite.Add(1)
			if count == maxIndefiniteTriggerCount {
				// Reset the oneshot counter
				indefinite.Store(0)
				log.Println("Reached max indefinite job count, scheduling new jobs...")
				// Send signal to start scheduling another batch of indefinite jobs
				indefiniteDoneCh <- struct{}{}
			}
		} else if strings.Contains(in.GetMethod(), "repeat-job") {
			count := repeat.Add(1)
			if count == maxRepeatTriggerCount {
				// Reset the repeat counter
				repeat.Store(0)
				log.Println("Reached max repeat job count, scheduling new jobs...")
				// Send signal to start scheduling another repeat job
				repeatDoneCh <- struct{}{}
			}
		} else if strings.Contains(in.GetMethod(), "create-delete-job") {
			count := createDelete.Add(1)
			log.Printf("create-delete-job triggered count: %d\n", count)

			if count == maxCreateDeleteTriggerCount {
				log.Println("Received the single create-delete-job, deleting it...")
				// Send signal to delete the job
				receivedSingleJobDoneCh <- struct{}{}
			} else {
				log.Printf("Received too many single repeat, create-delete-job jobs. Count: %d...\n", createDelete.Load())
			}
		}
	}
	return nil, nil
}

func scheduleOneshotJobs(ctx context.Context, daprClient client.Client) {
	for i := 0; i < 100; i++ {
		select {
		case <-ctx.Done():
			log.Println("context canceled, stopping scheduleOneshotJobs.")
			return
		default:
		}
		jobCtx, cancel := context.WithTimeout(ctx, 5*time.Second)
		name := "oneshot-job-" + strconv.Itoa(i)
		req := &client.Job{
			Name:     name,
			Schedule: "@every 30s",
			Repeats:  1, // one shot job
			DueTime:  "5s",
			TTL:      "40s",
			Data:     nil,
		}
		err := daprClient.ScheduleJobAlpha1(jobCtx, req)
		cancel()
		if err != nil {
			log.Printf("Error scheduling oneshot job '%s': %s\n", name, err)
		}
	}
}

func scheduleIndefiniteJobs(ctx context.Context, daprClient client.Client) {
	for i := 0; i < 100; i++ {
		select {
		case <-ctx.Done():
			log.Println("context canceled, stopping scheduleOneshotJobs.")
			return
		default:
		}
		jobCtx, cancel := context.WithTimeout(ctx, 5*time.Second)
		name := "indefinite-job-" + strconv.Itoa(i)
		req := &client.Job{
			Name:     name,
			Schedule: "@every 30s",
			DueTime:  "1s",
			TTL:      "40s",
			Data:     nil,
		}
		err := daprClient.ScheduleJobAlpha1(jobCtx, req)
		cancel()
		if err != nil {
			log.Printf("Error scheduling indefinite job '%s': %s\n", name, err)
		}
	}
}

func scheduleRepeatedJob(ctx context.Context, daprClient client.Client) {
	name := "repeat-job"
	req := &client.Job{
		Name:     name,
		Schedule: "@every 1s",
		DueTime:  "0s",
		Repeats:  maxRepeatTriggerCount,
		TTL:      "10s",
		Data:     nil,
	}
	jobCtx, cancel := context.WithTimeout(ctx, 5*time.Second)
	defer cancel()
	err := daprClient.ScheduleJobAlpha1(jobCtx, req)
	if err != nil {
		log.Printf("Error scheduling repeat job '%s': %s\n", name, err)
	}
}

func scheduleSingleJob(ctx context.Context, daprClient client.Client) {
	name := "create-delete-job"
	req := &client.Job{
		Name:     name,
		Schedule: "@every 1s",
		DueTime:  "1s",
		Repeats:  maxCreateDeleteTriggerCount,
		TTL:      "3s",
		Data:     nil,
	}
	jobCtx, cancel := context.WithTimeout(ctx, 5*time.Second)
	defer cancel()
	err := daprClient.ScheduleJobAlpha1(jobCtx, req)
	if err != nil {
		log.Printf("Error scheduling single job '%s': %s\n", name, err)
	}
}

func deleteSingleJob(ctx context.Context, daprClient client.Client) {
	name := "create-delete-job"
	jobCtx, cancel := context.WithTimeout(ctx, 5*time.Second)
	defer cancel()
	err := daprClient.DeleteJobAlpha1(jobCtx, name)
	if err != nil {
		log.Printf("Error deleting single job '%s': %s\n", name, err)
	}
	createDelete.Store(0)
	// signal to start the create/delete process again after waiting 2 minutes
	createDeleteDoneCh <- struct{}{}
}

func main() {
	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	go func(ctx context.Context) {
		if err := startServer(ctx); err != nil {
			log.Fatalf("Error starting server: %v", err)
		}
	}(ctx)

	daprClient, err := client.NewClient()
	if err != nil {
		log.Fatalf("Error getting dapr client: %v", err)
	}
	defer daprClient.Close()

	// allow time for sidecar to connect otherwise if the job is scheduled beforehand then it gets dropped on the
	// scheduler side bc there's no connection to send the job back on
	log.Println("waiting a few seconds to let connections establish")
	time.Sleep(5 * time.Second)

	// Schedule initial batch of jobs
	go scheduleOneshotJobs(ctx, daprClient)
	go scheduleIndefiniteJobs(ctx, daprClient)
	go scheduleRepeatedJob(ctx, daprClient)

	// Schedule additional oneshot jobs once 100 are triggered
	go func(ctx context.Context) {
		for {
			select {
			case <-ctx.Done():
				log.Println("context canceled, stopping oneshot scheduling goroutine.")
				return
			case <-oneshotDoneCh:
				log.Println("Received input that maxOneshotTriggerCount was reached. Sleeping...")
				time.Sleep(10 * time.Second)
				log.Println("Scheduling next batch of oneshot jobs...")
				go scheduleOneshotJobs(ctx, daprClient)
			}
		}
	}(ctx)

	// Schedule additional indefinite jobs once 100 are triggered
	go func(ctx context.Context) {
		for {
			select {
			case <-ctx.Done():
				log.Println("context canceled, stopping indefinite scheduling goroutine.")
				return
			case <-indefiniteDoneCh:
				log.Println("Received input that maxIndefiniteTriggerCount was reached. Sleeping...")
				time.Sleep(10 * time.Second)
				log.Println("Scheduling next batch of indefinite jobs...")
				go scheduleIndefiniteJobs(ctx, daprClient)
			}
		}
	}(ctx)

	// Schedule job to trigger immediately every second for 1s for 5 times max (repeats)
	go func(ctx context.Context) {
		for {
			select {
			case <-ctx.Done():
				return
			case <-repeatDoneCh:
				log.Println("Received input that maxRepeatTriggerCount was reached. Sleeping...")
				time.Sleep(60 * time.Second)
				log.Println("Scheduling next repeated job...")
				go scheduleRepeatedJob(ctx, daprClient)
			}
		}
	}(ctx)

	// Handle receivedSingleJobDoneCh, this handles the scheduled job from the next go routine
	go func(ctx context.Context) {
		for {
			select {
			case <-ctx.Done():
				return
			case <-receivedSingleJobDoneCh:
				log.Println("Received input that the create-delete-job triggered, now deleting the job...")
				// received triggered job, now delete it & set atomic int to 0
				deleteSingleJob(ctx, daprClient)
				log.Println("Successfully deleted create-delete-job.")
			}
		}
	}(ctx)

	go scheduleSingleJob(ctx, daprClient)

	// Reschedule the create-delete job after deletion, ensure triggers once
	go func(ctx context.Context) {
		for {
			select {
			case <-ctx.Done():
				return
			case <-createDeleteDoneCh:
				log.Println("Received input that the create-delete-job was deleted. Sleeping for 5 seconds...")
				time.Sleep(5 * time.Second)
				log.Println("Scheduling create-delete-job...")
				scheduleSingleJob(ctx, daprClient)
				log.Println("Successfully scheduled create-delete-job.")
			}
		}
	}(ctx)

	// Block until ctrl-c or sigterm
	waitForShutdown(cancel)
}

// waitForShutdown keeps the app alive until an interrupt or termination signal is received
func waitForShutdown(cancelFunc context.CancelFunc) {
	sigCh := make(chan os.Signal, 1)
	// Notify the channel on Interrupt (Ctrl+C) or SIGTERM (for Docker/K8s graceful shutdown)
	signal.Notify(sigCh, os.Interrupt, syscall.SIGTERM)

	// Block until we receive a signal
	<-sigCh

	log.Println("Shutting down...")
	cancelFunc()
}
