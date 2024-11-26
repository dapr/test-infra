package api

import (
	"context"
)

const PlayerActorType = "playerActorType"

type ClientStub struct {
	ActorID       string
	OnActivate    func(context.Context) error
	GetUser       func(ctx context.Context) (*GetPlayerResponse, error)
	Invoke        func(context.Context, string) (string, error)
	RevivePlayer  func(context.Context, string) error
	StartReminder func(context.Context, *ReminderRequest) error
	StopReminder  func(context.Context, *ReminderRequest) error
	ReminderCall  func(string, []byte, string, string) error
}

func (a *ClientStub) Type() string {
	return PlayerActorType
}

func (a *ClientStub) ID() string {
	return a.ActorID
}

type GetPlayerRequest struct {
	ActorID string
}

type GetPlayerResponse struct {
	ActorID string
	Health  int
}

type ReminderRequest struct {
	ReminderName string `json:"reminder_name"`
	DueTime      string `json:"due_time"`
	Period       string `json:"period"`
	Data         string `json:"data"`
}
