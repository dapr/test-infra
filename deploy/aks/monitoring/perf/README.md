## Performance Test Dashboard Setup

This document serves as a guideline for setting up a performance test dashboard using Azure managed Prometheus and Grafana. We will be creating an AKS (Azure Kubernetes Service) cluster and installing Prometheus Pushgateway and Azure Monitoring Agent (AMA) to scrape performance test metrics and push them to Azure managed Prometheus. Additionally, we will set up an ingress controller with authentication to act as a proxy for Prometheus Pushgateway. Finally, we will create a Grafana dashboard by importing a predefined JSON model.

Below are step-by-step instructions to set up a performance test dashboard using Azure managed Prometheus and Grafana. Follow these steps to configure your performance monitoring environment.

#### Step 1: Clone the test-infra repo

```bash
git clone https://github.com/dapr/test-infra.git
cd deploy/aks/monitoring/perf
```

#### Step 2: Login to Azure

```bash
az login
```

### Step 3: Set resource group name, location and use the same namespace (dapr-perf-metrics)

```bash
DAPR_PERF_RG={reosuceGroup}
DAPR_PERF_LOCATION={region}
DAPR_PERF_METRICS_NAMESPACE=dapr-perf-metrics
```

#### Step 4: Create Resource Group
```bash
az group create --name $DAPR_PERF_RG --location $DAPR_PERF_LOCATION
```

#### Step 5: Execute main.bicep and provide AKS cluster name on prompt

```bash
az deployment group create --resource-group $DAPR_PERF_RG --template-file main.bicep
```

#### Step 6: Merge Newly Created Cluster Username and Password

```bash
az aks get-credentials --resource-group $DAPR_PERF_RG --name {clusterName}
```

#### Step 7: Switch AKS Cluster Context

```bash
kubectl config use-context {clusterName}
```

#### Step 8: Install Prometheus Pushgateway

```bash
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update
helm install --namespace $DAPR_PERF_METRICS_NAMESPACE prometheus-pushgateway prometheus-community/prometheus-pushgateway
```

#### Step 9: Install Ingress Controller. 

Follow this [linke](https://learn.microsoft.com/en-us/azure/aks/ingress-basic?tabs=azure-cli#basic-configuration) for more details on setting up nginx ingress controller.

```bash
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update

helm install ingress-nginx ingress-nginx/ingress-nginx \
  --namespace $DAPR_PERF_METRICS_NAMESPACE \
  --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-health-probe-request-path"=/healthz
```

#### Step 10: Create Username and Password for Authentication

To create a basic authentication [username and password](https://kubernetes.github.io/ingress-nginx/examples/auth/basic/), use the following command, which will create an auth file and prompt you to provide a username and password.

```bash
htpasswd -c auth {userName}
```

#### Step 11: Create a Secret in Kubernetes

```bash
k create secret generic basic-auth --from-file=auth -n dapr-perf-metrics
```

#### Step 12: Create Ingress for Prometheus Pushgateway

```bash
k apply -f ./prometheus-pushgateway-ingress.yaml
```

#### Step 13: Create a Config Map for Service Discovery for AMA Agent

```bash
k apply -f ./prometheus-pushgateway-configmap.yaml
```

#### Step 14: Add user to grafana

- Go to grafana resource in Azure portal
- Select Access control (IAM) on left menu
- Click on Add role assignment
- Select suitable role for the user, and click Next
- In the Member tab, click on `+ Select Member` and type their email in search box
- Select user and click on `Review + assign`

#### Step 15: Create Grafana Dashboard

Grab the granfa link from azure portal and create a Grafana dashboard by importing the [JSON model](https://github.com/dapr/dapr/blob/master/tests/grafana/grafana-perf-test-dashboard.json). Ensure to update the `uid` in the JSON to match your configuration.
