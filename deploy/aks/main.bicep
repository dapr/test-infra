// Global parameters
param location string = resourceGroup().location
param clusterName string
param agentVMSize string = 'standard_d2s_v5'
param identityName string
param storageAccountName string
param serviceBusNamespace string
param kubernetesNamespace string = 'longhaul-test'
param grafanaName string
param amwName string
param logAnalyticsName string
param queueName string
param userGrafanaAdminObjectId string = ''

@description('The unique name of the solution. This is used to ensure that resource names are unique.')
@minLength(5)
@maxLength(30)
param solutionName string = 'lh${uniqueString(resourceGroup().id)}'

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
// TODO(tmacam) Doesn't this need to be a dependency of all the dapr components?
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
  name: '${deployment().name}--services--storage'
  params: {
    //solutionName: solutionName
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
  name: '${deployment().name}--services--cosmos'
  params: {
    solutionName: solutionName
    principalId: managedIdentity.properties.principalId
    location: location
  }
  dependsOn: [
    longhaulNamespace
  ]
}

module cosmosComponent 'daprComponents/cosmos-component.bicep' = {
  name: '${deployment().name}--component--cosmos'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
    cosmosUrl: cosmos.outputs.cosmosUrl
    cosmosContainerName: cosmos.outputs.cosmosContainerName
    cosmosDatabaseName: cosmos.outputs.cosmosDatabaseName
    cosmosAccountPrimaryMasterKey: cosmos.outputs.cosmosAccountPrimaryMasterKey
  }
  dependsOn: [
    cosmos
  ]
}

module messageBindingComponent 'daprComponents/storage-queue-component.bicep' = {
  name: '${deployment().name}--component--storageQueue'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
    storageAccountKey: storageServices.outputs.storageAccountKey
    storageAccountName: storageServices.outputs.storageAccountName
    storageQueueName: storageServices.outputs.storageQueueName
  }
  dependsOn: [
    storageServices
  ]
}

//
//  Longhaul test applications
//

// Servicebus
module servicebus 'services/servicebus.bicep' = {
  name: '${deployment().name}--services--servicebus'
  params: {
    serviceBusNamespace: serviceBusNamespace
    principalId: managedIdentity.properties.principalId
    location: location
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
  }
  dependsOn: [
    daprExtension
    longhaulNamespace
  ]
}

module servicebusComponent 'daprComponents/servicebus-pubsub-component.bicep' = {
  name: '${deployment().name}--component--servicebus'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
    serviceBusConnectionString: servicebus.outputs.serviceBusConnectionString
  }
  dependsOn: [
    servicebus
  ]
}


// Apps

// TODO(tmacam) figure out inter-app dependency and update dependsOn list
module feedGenerator 'apps/feed-generator-deploy.bicep' = {
  name: '${deployment().name}--app--feed-generator'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
  dependsOn: [
    daprExtension
    longhaulNamespace
    servicebusComponent
  ]
}

module messageAnalyzer 'apps/message-analyzer-deploy.bicep' = {
  name: '${deployment().name}--app--message-analyzer'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
  dependsOn: [
    daprExtension
    longhaulNamespace
    messageBindingComponent
    servicebusComponent
  ]
}

module hashtagActor 'apps/hashtag-actor-deploy.bicep' = {
  name: '${deployment().name}--app--hashtag-actor'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
  dependsOn: [
    daprExtension
    longhaulNamespace
    cosmosComponent
  ]
}

module hashtagCounter 'apps/hashtag-counter-deploy.bicep' = {
  name: '${deployment().name}--app--hashtag-counter'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
  dependsOn: [
    daprExtension
    longhaulNamespace
    storageServices
    hashtagActor
  ]
}



module pubsubWorkflowApp 'apps/pubsub-workflow-deploy.bicep' = {
  name: '${deployment().name}--app--pubsub-workflow'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
  dependsOn: [
    daprExtension
    longhaulNamespace
    servicebusComponent
  ]
}

module snapshotApp 'apps/snapshot-deploy.bicep' = {
  name: '${deployment().name}--app--snapshot'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
  dependsOn: [
    daprExtension
    longhaulNamespace
    servicebusComponent
    hashtagActor
  ]
}

module validationWorker 'apps/validation-worker-deploy.bicep' = {
  name: '${deployment().name}--app--validation-worker'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
  dependsOn: [
    daprExtension
    longhaulNamespace
    snapshotApp
  ]
}
