## Performance Test Dashboard Setup

This document serves as a guideline for setting up a performance test dashboard using Azure managed Prometheus and Grafana. We will be creating an AKS (Azure Kubernetes Service) cluster and installing Prometheus Pushgateway and Azure Monitoring Agent (AMA) to scrape performance test metrics and push them to Azure managed Prometheus. Additionally, we will set up an ingress controller with authentication to act as a proxy for Prometheus Pushgateway. Finally, we will create a Grafana dashboard by importing a predefined JSON model.

Below are step-by-step instructions to set up a performance test dashboard using Azure managed Prometheus and Grafana. Follow these steps to configure your performance monitoring environment.

#### Step 1: Login to Azure

```bash
az login
```

#### Step 2: Create Resource Group
```bash
az group create --name {resourceGroup} --location {region}
```

#### Step 3: Execute main.bicep

```bash
az deployment group create --resource-group {resourceGroup} --template-file perf-test-dashboard.bicep
```

#### Step 4: Merge Newly Created Cluster Username and Password

```bash
az aks get-credentials --resource-group {resourceGroup} --name {clusterName}
```

#### Step 5: Switch AKS Cluster Context

```bash
kubectl config use-context {clusterName}
```

#### Step 6: Install Prometheus Pushgateway

```bash
DAPR_PERF_METRICS_NAMESPACE=dapr-perf-metrics
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update
helm install --namespace $DAPR_PERF_METRICS_NAMESPACE prometheus-pushgateway prometheus-community/prometheus-pushgateway
```

#### Step 7: Install Ingress Controller. 

Follow this [linke](https://learn.microsoft.com/en-us/azure/aks/ingress-basic?tabs=azure-cli#basic-configuration) for more details on setting up nginx ingress controller.

```bash
DAPR_PERF_METRICS_NAMESPACE=dapr-perf-metrics

helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update

helm install ingress-nginx ingress-nginx/ingress-nginx \
  --namespace $DAPR_PERF_METRICS_NAMESPACE \
  --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-health-probe-request-path"=/healthz
```

#### Step 8: Create Username and Password for Authentication

To create a basic authentication [username and password](https://kubernetes.github.io/ingress-nginx/examples/auth/basic/), use the following command, which will create an auth file and prompt you to provide a username and password.

```bash
htpasswd -c auth {userName}
```

#### Step 9: Create a Secret in Kubernetes

```bash
k create secret generic basic-auth --from-file=auth -n dapr-perf-metrics
```

#### Step 10: Create Ingress for Prometheus Pushgateway

```bash
k apply -f ./prometheus-pushgateway-ingress.yaml
```

#### Step 11: Create a Config Map for Service Discovery for AMA Agent

```bash
k apply -f ./service-discovery-config.yaml
```

#### Step 12: Create Grafana Dashboard

Grab the granfa link from azure portal and create a Grafana dashboard by importing the [JSON model](https://github.com/dapr/dapr/blob/master/tests/grafana/grafana-perf-test-dashboard.json). Ensure to update the `uid` in the JSON to match your configuration.
