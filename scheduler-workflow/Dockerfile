FROM golang:1.23 as builder

# Working directory inside the container
WORKDIR /scheduler-workflow

COPY go.mod go.sum ./
RUN go mod download

# Copy the app code
COPY . .

# Build the app
RUN CGO_ENABLED=0 GOOS=linux go build -o scheduler-workflow .

# Initialize a new build stage. Use a minimal base image for the final container
FROM alpine:latest

# Install certificates for HTTPS support
RUN apk --no-cache add ca-certificates

# Copy the binary from the builder image -> final image
COPY --from=builder /scheduler-workflow/scheduler-workflow /usr/local/bin/scheduler-workflow

# Port the app will listen on
EXPOSE 3009

# Entrypoint to run the app
ENTRYPOINT ["scheduler-workflow"]
