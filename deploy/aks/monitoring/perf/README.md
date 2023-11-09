## Performance Test Dashboard Setup

This document serves as a guideline for setting up a performance test dashboard using Azure managed Prometheus and Grafana. We will be creating an AKS (Azure Kubernetes Service) cluster and installing Prometheus Pushgateway and Azure Monitoring Agent (AMA) to scrape performance test metrics and push them to Azure managed Prometheus. Additionally, we will set up an ingress controller with authentication to act as a proxy for Prometheus Pushgateway. Finally, we will create a Grafana dashboard by importing a predefined JSON model.

Below are step-by-step instructions to set up a performance test dashboard using Azure managed Prometheus and Grafana. Follow these steps to configure your performance monitoring environment.

#### Step 1: Clone the test-infra repo

```bash
git clone https://github.com/dapr/test-infra.git
cd deploy/aks/monitoring/perf
```

#### Step 2: Login to Azure and set your subscription

```bash
az login
export SUBSCRIPTION_ID=<SUBSCRIPTION UUID TO BE USED FOR THE COMMANDS BELLOW>
az account set --subscription "${SUBSCRIPTION_ID}"
```

### Step 3: Set resource group name, location, cluster name and use the same namespace (dapr-perf-metrics)

```bash
DAPR_PERF_RG=<resource group to be used>  
DAPR_PERF_LOCATION=<insert region>  
CLUSTER_NAME=<cluster name>
DAPR_PERF_METRICS_NAMESPACE=dapr-perf-metrics
```

#### Step 4: Create Resource Group
```bash
az group create --name $DAPR_PERF_RG --location $DAPR_PERF_LOCATION
```

#### Step 5: Execute main.bicep and provide AKS cluster name on prompt

```bash
az deployment group create --resource-group $DAPR_PERF_RG --template-file main.bicep --parameters clusterName="${CLUSTER_NAME}"
```

#### Step 6: Merge Newly Created Cluster Username and Password

```bash
az aks get-credentials --resource-group $DAPR_PERF_RG --name "${CLUSTER_NAME}"
```

#### Step 7: Switch AKS Cluster Context

```bash
kubectl config use-context "${CLUSTER_NAME}"
```

#### Step 8: Install Prometheus Pushgateway

```bash
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update
helm upgrade --install \
    prometheus-pushgateway prometheus-community/prometheus-pushgateway \
    --namespace $DAPR_PERF_METRICS_NAMESPACE \
    --create-namespace \
    --wait
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
htpasswd -c auth <user name for prometheus pushgateway>
```

#### Step 11: Create a Secret in Kubernetes

```bash
kubectl create secret generic basic-auth --from-file=auth -n dapr-perf-metrics
```

#### Step 12: Create Ingress for Prometheus Pushgateway

```bash
kubectl apply -f ./prometheus-pushgateway-ingress.yaml
```

#### Step 13: Create a Config Map for Service Discovery for AMA Agent

```bash
kubectl apply -f ./prometheus-pushgateway-configmap.yaml
```

#### Step 14: Add user to grafana

- Go to grafana resource in Azure portal
- Select Access control (IAM) on left menu
- Click on Add role assignment
- Select suitable role for the user, and click Next
- In the Member tab, click on `+ Select Member` and type their email in search box
- Select user and click on `Review + assign`

#### Step 15: Create Grafana Dashboard

Grab the granfa link from azure portal and create a Grafana dashboard by importing the [JSON model](https://github.com/dapr/dapr/blob/78b7271f015fa935fd59299357787f3e86861300/tests/grafana/grafana-perf-test-dashboard.json). Ensure to update all [`uid` of `datasource`](https://github.com/dapr/dapr/blob/78b7271f015fa935fd59299357787f3e86861300/tests/grafana/grafana-perf-test-dashboard.json#L41) objects present in the JSON file to match your configuration.
