Step 1: Login to Azure

```bash
az login
```

step 2 : Create resource group
```bash
az group create --name perf-test-resource-rg-1 --location eastus
```

step 3 : Execute main.bicep

```bash
az deployment group create --resource-group perf-test-resource-rg-1 --template-file perf-test-dashboard.bicep
```

step 4 : Merge newly created cluster username password to 

```bash
az aks get-credentials --resource-group perf-test-resource-rg-1 --name pt-cluster-1
```

step 5: switch context

```bash
kubectl config use-context pt-cluster-1
```

step 5 : install prometheus-pushgateway

```bash
DAPR_PERF_METRICS_NAMESPACE=dapr-perf-metrics
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update
helm install --namespace $DAPR_PERF_METRICS_NAMESPACE prometheus-pushgateway prometheus-community/prometheus-pushgateway
```

step 5 : Install ingress controller. Follow this [linke](https://learn.microsoft.com/en-us/azure/aks/ingress-basic?tabs=azure-cli#basic-configuration) for more details

```bash
DAPR_PERF_METRICS_NAMESPACE=dapr-perf-metrics

helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update

helm install ingress-nginx ingress-nginx/ingress-nginx \
  --namespace $DAPR_PERF_METRICS_NAMESPACE \
  --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-health-probe-request-path"=/healthz
```

step 6: Create [username and password](https://kubernetes.github.io/ingress-nginx/examples/auth/basic/) for authentication, below command will create an auth file and ask you to provide username and password

```bash
htpasswd -c auth foo
```

step 7 : create secret in k8s

```bash
k create secret generic basic-auth --from-file=auth -n dapr-perf-metrics
```

step 8 : create ingress for prometheus-pushgateway

```bash
k apply -f ./prometheus-pushgateway-ingress.yaml
```

step 9: create a config map for service discovery for ama agen

```bash
k apply -f ./service-discovery-config.yaml
```