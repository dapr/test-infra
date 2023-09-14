# Dapr Test Infra

This repo includes test apps and infrastructure tools.

Test apps for long haul tests:
* [Feed Generator](./feed-generator) : contains feed-generator logic. Exercises the pubsub component (publishing).
* [Message Analyzer](./message-analyzer) : contains message-analyzer logic. Exercises pubsub component (subscribing) and output binding.
* [HashTag Counter](./hashtag-counter) : contains hashtag-counter. Exercises input binding and actor component.
* [HashTag Actor](./hashtag-actor) : hashtag-actor logic. Exercises actor component. 
* [Pubsub Workflow](./pubsub-workflow) : azure service bus pubsub logic. Exercises pubsub component (publishing and subscribing).

Test analytics :
* [Test Crawler](./test-crawler) : A Python script scrapes the Dapr E2E tests results.

# Solution overview and app dependency

```mermaid
sequenceDiagram
FeedGenerator --) MessageAnalyzer : publish a SocialMediaMessage <br> async using pubsub <br> topic "receivemediapost"
MessageAnalyzer --) HashTagCounter: invoke output binding "messagebinding" <br> to publish a message to a queue <br> shared with HashTagCounter
HashTagCounter ->> HashTagActor: receives messages from shared queue <br> using input binding "messagebinding" <br> increase counter by invoking a <br> per (hastag,sentiment) actor
Snapshot ->> HashTagActor: get counts from HashTagActor
PubSubWorkflow --) PubSubWorkflow: publish messages at different rates <br> to different topics it also subscribes to.
```

TODO what about Pubsub Workflow ? who is publishing to it? And validation workflow?

# Deploying this application

## Locally with Dapr Multi-App run

This is not properly a deployment, but it is a way to run all the longhaul applications locally using [Dapr Multi-App run](https://docs.dapr.io/developing-applications/local-development/multi-app-dapr-run/).


```bash	
# Start base components such as redis and zipkin, which we depend on
dapr init
# Start RabbitMQ which is used for our input/output binding tests.
docker run -d -p 5672:5672 --hostname dapr_rabbitmq --name dapr_rabbitmq rabbitmq:3-alpine
# Run all the apps
dapr run -f deploy/dapr-multi-app/dapr.yaml
```

## On Azure Kubernetes Service (AKS)

Define the resource group we in which will we create the cluster and other resources and
the name of the cluster we will create.

```bash
export resourceGroup='myNewResourceGroup'
export location='eastus'
export clusterName='test-infra'
```

Create a resource group four your new cluster

```bash
az group create --name ${resourceGroup} --location ${location}
```

Deploy a cluster with test apps to this resource group:

```bash
az deployment group create --resource-group ${resourceGroup} --template-file ./deploy/aks/main.bicep --parameters "clusterName=${clusterName}"
```


Done! Explore your new AKS cluster with the sample applications

```bash
az aks get-credentials --admin --name ${clusterName} --resource-group ${resourceGroup}
```

## On Azure Container Apps (ACA)