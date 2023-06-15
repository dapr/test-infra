// Global parameters
param location string = resourceGroup().location
param clusterName string
param agentVMSize string = 'standard_d2as_v4'
param identityName string
param storageAccountName string
param cosmosAccountName string
param serviceBusNamespace string
param kubernetesNamespace string
param grafanaName string
param amwName string
param logAnalyticsName string
param queueName string
param userGrafanaAdminObjectId string = ''

// Identity - Not a module so we can reference the resource below.
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

// AKS Cluster - Not a module so we can reference the resource below.
resource aks 'Microsoft.ContainerService/managedClusters@2023-03-01' = {
  name: clusterName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    dnsPrefix: '${clusterName}-dns'
    agentPoolProfiles: [
      {
        name: 'agentpool'
        count: 3
        vmSize: agentVMSize
        osType: 'Linux'
        mode: 'System'
      }
    ]
    azureMonitorProfile: {
      metrics: {
        enabled: true
        kubeStateMetrics: {
          metricLabelsAllowlist: '*'
        }
      }
    }
  }
}

// Dapr extension for the cluster.
resource daprExtension 'Microsoft.KubernetesConfiguration/extensions@2022-11-01' = {
  name: 'dapr-ext'
  scope: aks
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    extensionType: 'microsoft.dapr'
    scope: {
      cluster: {
        releaseNamespace: 'dapr-system'
      }
    }
  }
}

module monitoring 'monitoring/monitoring.bicep' = {
  name: '${deployment().name}--monitoring'
  params: {
    location: location
    clusterName: clusterName
    dceName: '${clusterName}-dce'
    dcrName: '${clusterName}-dcr'
    grafanaName: grafanaName
    workspaceAzureMonitorName: amwName
    workspaceLogAnalyticsName: logAnalyticsName
    grafanaAdminObjectId: managedIdentity.properties.principalId
    userGrafanaAdminObjectId: userGrafanaAdminObjectId
  }
}

// Apply the k8s namespace
module longhaulNamespace 'services/namespace.bicep' = {
  name: '${deployment().name}--namespace'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
}

// Azure Services & Components
// Blobstore
module storageServices 'services/storage-services.bicep' = {
  name: '${deployment().name}--storage-services'
  params: {
    accountName: storageAccountName
    principalId: managedIdentity.properties.principalId
    location: location
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
    queueName: queueName
  }
  dependsOn: [
    longhaulNamespace
  ]
}

// CosmosDB
module cosmos 'services/cosmos.bicep' = {
  name: '${deployment().name}--cosmos'
  params: {
    cosmosAccountName: cosmosAccountName
    principalId: managedIdentity.properties.principalId
    location: location
  }
  dependsOn: [
    longhaulNamespace
  ]
}

module cosmosComponent 'daprComponents/cosmos-component.bicep' = {
  name: '${deployment().name}--cosmosComponent'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
    cosmosAccountName: cosmos.outputs.cosmosAccountName
    cosmosUrl: cosmos.outputs.cosmosUrl
    cosmosContainerName: cosmos.outputs.cosmosContainerName
    cosmosDatabaseName: cosmos.outputs.cosmosDatabaseName
  }
  dependsOn: [
    cosmos
  ]
}

// Servicebus
module servicebus 'services/servicebus.bicep' = {
  name: '${deployment().name}--servicebus'
  params: {
    serviceBusNamespace: serviceBusNamespace
    principalId: managedIdentity.properties.principalId
    location: location
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
  dependsOn: [
    longhaulNamespace
  ]
}

// Apps
module feedGenerator 'apps/feed-generator-deploy.bicep' = {
  name: '${deployment().name}--feed-generator'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
  dependsOn: [
    longhaulNamespace
    servicebus
  ]
}

module hashtagActor 'apps/hashtag-actor-deploy.bicep' = {
  name: '${deployment().name}--hashtag-actor'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
  dependsOn: [
    longhaulNamespace
    cosmos
  ]
}

module hashtagCounter 'apps/hashtag-counter-deploy.bicep' = {
  name: '${deployment().name}--hashtag-counter'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
  dependsOn: [
    longhaulNamespace
    storageServices
  ]
}

module messageAnalyzer 'apps/message-analyzer-deploy.bicep' = {
  name: '${deployment().name}--message-analyzer'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
  dependsOn: [
    longhaulNamespace
    storageServices
    servicebus
  ]
}

module pubsubApp 'apps/pubsub-workflow-deploy.bicep' = {
  name: '${deployment().name}--pubsub-workflow'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
  dependsOn: [
    longhaulNamespace
    servicebus
  ]
}

module snapshot 'apps/snapshot-deploy.bicep' = {
  name: '${deployment().name}--snapshot'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
  dependsOn: [
    longhaulNamespace
    servicebus
  ]
}

module validationWorker 'apps/validation-worker-deploy.bicep' = {
  name: '${deployment().name}--validation-worker'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
  dependsOn: [
    longhaulNamespace
    snapshot
  ]
}
