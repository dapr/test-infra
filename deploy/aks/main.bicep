
//
// Global parameters
//

@minLength(5)
@maxLength(30)
@description('Name of the cluster')
param clusterName string

@minLength(1)
@maxLength(3)
@description('Short, up to 3 letters prefix to help identify cluster resources. Lowercase and numbers only')
param shortClusterPrefixId string = 'lh'


@description('The unique discriminator of the solution. This is used to ensure that resource names are unique.')
@minLength(3)
@maxLength(16)
param solutionName string = toLower('${shortClusterPrefixId}${uniqueString(resourceGroup().id)}')

// Per cluster resources
param identityName string = '${solutionName}-identity'
@maxLength(24)
param storageAccountName string = '${solutionName}storage'
param serviceBusNamespace string = '${solutionName}sb'
@minLength(2)
@maxLength(23)
param grafanaName string = '${solutionName}-grafan'
param amwName string = '${solutionName}-amw'
param logAnalyticsName string = '${solutionName}-la'

// Safe defaults
param agentVMSize string = 'standard_d2s_v5'
param location string = resourceGroup().location
param kubernetesNamespace string = 'longhaul-test'
param userGrafanaAdminObjectId string = 'b92bc7c4-11f0-4e38-b49f-58621e50c3e5'



// Identity - Not a module so we can reference the resource below.
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

//
// Cluster infrastructure
//


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
  name: '${clusterName}-dapr-ext'
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
  name: '${clusterName}--monitoring'
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
  // Interestingly, this can be deployed in parallel to AKS cluster and it works just fine. Go figure.
}

// Apply the k8s namespace - applications and CRDs live here
module longhaulNamespace 'services/namespace.bicep' = {
  name: '${clusterName}--namespace'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
}

//
// Azure Services & Components
//

// Blobstore
module blobstore 'services/storage-services.bicep' = {
  name: '${clusterName}--services--blobstore'
  params: {
    //solutionName: solutionName
    accountName: storageAccountName
    principalId: managedIdentity.properties.principalId
    location: location
  }
}

// CosmosDB
module cosmos 'services/cosmos.bicep' = {
  name: '${clusterName}--services--cosmos'
  params: {
    solutionName: solutionName
    principalId: managedIdentity.properties.principalId
    location: location
  }
}


// Servicebus
module servicebus 'services/servicebus.bicep' = {
  name: '${clusterName}--services--servicebus'
  params: {
    serviceBusNamespace: serviceBusNamespace
    principalId: managedIdentity.properties.principalId
    location: location
  }
}

//
// Dapr Components
//

module cosmosComponent 'daprComponents/cosmos-component.bicep' = {
  name: '${clusterName}--component--cosmos'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: longhaulNamespace.outputs.kubernetesNamespace
    cosmosUrl: cosmos.outputs.cosmosUrl
    cosmosContainerName: cosmos.outputs.cosmosContainerName
    cosmosDatabaseName: cosmos.outputs.cosmosDatabaseName
    cosmosAccountPrimaryMasterKey: cosmos.outputs.cosmosAccountPrimaryMasterKey
  }
  dependsOn: [
    cosmos
    daprExtension
    longhaulNamespace
  ]
}

module messageBindingComponent 'daprComponents/storage-queue-component.bicep' = {
  name: '${clusterName}--component--storageQueue'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: longhaulNamespace.outputs.kubernetesNamespace
    storageAccountKey: blobstore.outputs.storageAccountKey
    storageAccountName: blobstore.outputs.storageAccountName
    storageQueueName: blobstore.outputs.storageQueueName
  }
  dependsOn: [
    blobstore
    daprExtension
    longhaulNamespace
  ]
}



module servicebusComponent 'daprComponents/servicebus-pubsub-component.bicep' = {
  name: '${clusterName}--component--servicebus'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: longhaulNamespace.outputs.kubernetesNamespace
    serviceBusConnectionString: servicebus.outputs.serviceBusConnectionString
  }
  dependsOn: [
    servicebus
    daprExtension
    longhaulNamespace
  ]
}


//
//  Longhaul test applications
//


// TODO(tmacam) figure out inter-app dependency and update dependsOn list
module feedGenerator 'apps/feed-generator-deploy.bicep' = {
  name: '${clusterName}--app--feed-generator'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: longhaulNamespace.outputs.kubernetesNamespace
  }
  dependsOn: [
    daprExtension
    longhaulNamespace
    servicebusComponent
  ]
}

module messageAnalyzer 'apps/message-analyzer-deploy.bicep' = {
  name: '${clusterName}--app--message-analyzer'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: longhaulNamespace.outputs.kubernetesNamespace
  }
  dependsOn: [
    daprExtension
    longhaulNamespace
    messageBindingComponent
    servicebusComponent
  ]
}

module hashtagActor 'apps/hashtag-actor-deploy.bicep' = {
  name: '${clusterName}--app--hashtag-actor'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: longhaulNamespace.outputs.kubernetesNamespace
  }
  dependsOn: [
    daprExtension
    longhaulNamespace
    cosmosComponent
  ]
}

module hashtagCounter 'apps/hashtag-counter-deploy.bicep' = {
  name: '${clusterName}--app--hashtag-counter'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: longhaulNamespace.outputs.kubernetesNamespace
  }
  dependsOn: [
    daprExtension
    longhaulNamespace
    blobstore
    hashtagActor
  ]
}



module pubsubWorkflowApp 'apps/pubsub-workflow-deploy.bicep' = {
  name: '${clusterName}--app--pubsub-workflow'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: longhaulNamespace.outputs.kubernetesNamespace
  }
  dependsOn: [
    daprExtension
    longhaulNamespace
    servicebusComponent
  ]
}

module snapshotApp 'apps/snapshot-deploy.bicep' = {
  name: '${clusterName}--app--snapshot'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: longhaulNamespace.outputs.kubernetesNamespace
  }
  dependsOn: [
    daprExtension
    longhaulNamespace
    servicebusComponent
    hashtagActor
  ]
}

module validationWorker 'apps/validation-worker-deploy.bicep' = {
  name: '${clusterName}--app--validation-worker'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: longhaulNamespace.outputs.kubernetesNamespace
  }
  dependsOn: [
    daprExtension
    longhaulNamespace
    snapshotApp
  ]
}
