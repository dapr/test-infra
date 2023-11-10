## Performance Test Dashboard Setup

This document serves as a guideline for setting up a performance test dashboard using Azure managed Prometheus and Grafana. We will be creating an AKS (Azure Kubernetes Service) cluster and installing Prometheus Pushgateway and Azure Monitoring Agent (AMA) to scrape performance test metrics and push them to Azure managed Prometheus. Additionally, we will set up an ingress controller with authentication to act as a proxy for Prometheus Pushgateway. Finally, we will create a Grafana dashboard by importing a predefined JSON model.

Below are step-by-step instructions to set up a performance test dashboard using Azure managed Prometheus and Grafana. Follow these steps to configure your performance monitoring environment.

Below official docs were followed to
  - [Create authentication for ingress controller](https://kubernetes.github.io/ingress-nginx/examples/auth/basic/)
  - [Use TLS with an ingress controller](https://learn.microsoft.com/en-us/azure/aks/ingress-tls?tabs=azure-cli#configure-an-fqdn-for-your-ingress-controller)

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
PROMETHEUS_PUSHGATEWAY_USER_NAME=<user name for prometheus pushgateway>
DAPR_PERF_ACR_NAME=<container registry name>
DNSLABEL=<"Name to associate with public IP address">
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
    --wait
```

#### Step 9: Create ACR
```bash
az acr create -n ${DAPR_PERF_ACR_NAME} -g ${DAPR_PERF_RG} --sku basic
```

#### Step 10: Attach using acr-name
```bash
az aks update -n ${CLUSTER_NAME} -g ${DAPR_PERF_RG} --attach-acr ${DAPR_PERF_ACR_NAME}
```

#### Step 11: Install Ingress Controller. 

Follow this [link](https://learn.microsoft.com/en-us/azure/aks/ingress-basic?tabs=azure-cli#basic-configuration) for more details on setting up nginx ingress controller.

```bash
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update

helm upgrade --install \
    ingress-nginx ingress-nginx/ingress-nginx \
  --namespace $DAPR_PERF_METRICS_NAMESPACE \
  --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-health-probe-request-path"=/healthz
```

#### Step 12: Import the cert-manager images used by the Helm chart into your ACR
```bash
CERT_MANAGER_REGISTRY=quay.io
CERT_MANAGER_TAG=v1.8.0
CERT_MANAGER_IMAGE_CONTROLLER=jetstack/cert-manager-controller
CERT_MANAGER_IMAGE_WEBHOOK=jetstack/cert-manager-webhook
CERT_MANAGER_IMAGE_CAINJECTOR=jetstack/cert-manager-cainjector

az acr import --name $DAPR_PERF_ACR_NAME --source $CERT_MANAGER_REGISTRY/$CERT_MANAGER_IMAGE_CONTROLLER:$CERT_MANAGER_TAG --image $CERT_MANAGER_IMAGE_CONTROLLER:$CERT_MANAGER_TAG
az acr import --name $DAPR_PERF_ACR_NAME --source $CERT_MANAGER_REGISTRY/$CERT_MANAGER_IMAGE_WEBHOOK:$CERT_MANAGER_TAG --image $CERT_MANAGER_IMAGE_WEBHOOK:$CERT_MANAGER_TAG
az acr import --name $DAPR_PERF_ACR_NAME --source $CERT_MANAGER_REGISTRY/$CERT_MANAGER_IMAGE_CAINJECTOR:$CERT_MANAGER_TAG --image $CERT_MANAGER_IMAGE_CAINJECTOR:$CERT_MANAGER_TAG
```

#### Step 13: Configure an FQDN for ingress controller
```bash
# Public IP address of your ingress controller
IP=$(kubectl get service ingress-nginx-controller -n $DAPR_PERF_METRICS_NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].ip}')

# Get the resource-id of the public IP
PUBLICIPID=$(az network public-ip list --query "[?ipAddress!=null]|[?contains(ipAddress, '$IP')].[id]" --output tsv)

# Update public IP address with DNS name
az network public-ip update --ids $PUBLICIPID --dns-name $DNSLABEL

# Display the FQDN
az network public-ip show --ids $PUBLICIPID --query "[dnsSettings.fqdn]" --output tsv
```

#### Step 14: Set the DNS label using Helm chart settings
```bash
helm upgrade ingress-nginx ingress-nginx/ingress-nginx \
  --namespace $DAPR_PERF_METRICS_NAMESPACE \
  --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-dns-label-name"=$DNSLABEL \
  --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-health-probe-request-path"=/healthz
```

#### Step 15: Install cert-manager
```bash
# Set variable for ACR location to use for pulling images
ACR_URL=$(az acr show --name ${DAPR_PERF_ACR_NAME} --resource-group ${DAPR_PERF_RG} --query "loginServer" --output tsv)

# Label the $DAPR_PERF_METRICS_NAMESPACE namespace to disable resource validation
kubectl label namespace $DAPR_PERF_METRICS_NAMESPACE cert-manager.io/disable-validation=true

# Add the Jetstack Helm repository
helm repo add jetstack https://charts.jetstack.io

# Update your local Helm chart repository cache
helm repo update

# Install the cert-manager Helm chart
helm install cert-manager jetstack/cert-manager \
  --namespace $DAPR_PERF_METRICS_NAMESPACE \
  --version=$CERT_MANAGER_TAG \
  --set installCRDs=true \
  --set nodeSelector."kubernetes\.io/os"=linux \
  --set image.repository=$ACR_URL/$CERT_MANAGER_IMAGE_CONTROLLER \
  --set image.tag=$CERT_MANAGER_TAG \
  --set webhook.image.repository=$ACR_URL/$CERT_MANAGER_IMAGE_WEBHOOK \
  --set webhook.image.tag=$CERT_MANAGER_TAG \
  --set cainjector.image.repository=$ACR_URL/$CERT_MANAGER_IMAGE_CAINJECTOR \
  --set cainjector.image.tag=$CERT_MANAGER_TAG
```

#### Step 16: Create a CA cluster issuer - Do not forget to provide email address in [cluster-issuer.yaml](./cluster-issuer.yaml)
```bash
kubectl apply -f cluster-issuer.yaml --namespace $DAPR_PERF_METRICS_NAMESPACE
```

#### Step 17: Create Username and Password for Authentication

To create a basic authentication [username and password](https://kubernetes.github.io/ingress-nginx/examples/auth/basic/), use the following command, which will create an auth file and prompt you to provide a username and password.

```bash
htpasswd -c auth ${PROMETHEUS_PUSHGATEWAY_USER_NAME}
```

#### Step 18: Create a Secret in Kubernetes

```bash
kubectl create secret generic basic-auth --from-file=auth -n ${DAPR_PERF_METRICS_NAMESPACE}
```

#### Step 19: Create Ingress for Prometheus Pushgateway. Do not forget to replace `hello-world-ingress.MY_CUSTOM_DOMAIN` with your FQDN [here](./prometheus-pushgateway-ingress.yaml). Your FQDN should follow this form: `<CUSTOM DNS LABEL>.<AZURE REGION NAME>.cloudapp.azure.com`.

```bash
kubectl apply -f ./prometheus-pushgateway-ingress.yaml
```

#### Step 20: Create a Config Map for Service Discovery for AMA Agent

```bash
kubectl apply -f ./prometheus-pushgateway-configmap.yaml
```

#### Step 21: Add user to grafana

- Go to grafana resource in Azure portal
- Select Access control (IAM) on left menu
- Click on Add role assignment
- Select suitable role for the user, and click Next
- In the Member tab, click on `+ Select Member` and type their email in search box
- Select user and click on `Review + assign`

#### Step 22: Create Grafana Dashboard

Grab the granfa link from azure portal and create a Grafana dashboard by importing the [JSON model](https://github.com/dapr/dapr/blob/78b7271f015fa935fd59299357787f3e86861300/tests/grafana/grafana-perf-test-dashboard.json). Ensure to update all [`uid` of `datasource`](https://github.com/dapr/dapr/blob/78b7271f015fa935fd59299357787f3e86861300/tests/grafana/grafana-perf-test-dashboard.json#L41) objects present in the JSON file to match your configuration.
