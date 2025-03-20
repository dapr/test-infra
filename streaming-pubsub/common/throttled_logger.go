package logger

import (
	"sync"
	"time"
)

// ThrottledLogger provides a way to throttle log messages so they only appear
// once per specified duration so as not to increase resource consumption in the longhauls cluster.
type ThrottledLogger struct {
	lastLogTime sync.Map
	interval    time.Duration
}

func NewThrottledLogger(interval time.Duration) *ThrottledLogger {
	return &ThrottledLogger{
		lastLogTime: sync.Map{},
		interval:    interval,
	}
}

func (t *ThrottledLogger) ShouldLog(key string) bool {
	now := time.Now()

	value, exists := t.lastLogTime.Load(key)

	if !exists {
		t.lastLogTime.Store(key, now)
		return true
	}

	lastTime := value.(time.Time)
	if now.Sub(lastTime) >= t.interval {
		t.lastLogTime.Store(key, now)
		return true
	}

	return false
}
