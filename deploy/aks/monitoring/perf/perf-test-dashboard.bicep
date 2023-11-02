
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
@minLength(2)
@maxLength(23)
param grafanaName string = '${solutionName}-grafan'
param amwName string = '${solutionName}-amw'
param logAnalyticsName string = '${solutionName}-la'

// Safe defaults
param agentVMSize string = 'standard_d2s_v5'
param location string = resourceGroup().location
param kubernetesNamespace string = 'dapr-perf-metrics'
@description('ObjectID for an user in AAD you want to grant grafana admin rights. Default is to not provide anything: not grant this permission any individual')
param userGrafanaAdminObjectId string = ''



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

// Apply the k8s namespace - applications and CRDs live here
module longhaulNamespace '../../services/namespace.bicep' = {
  name: '${clusterName}--namespace'
  params: {
    kubeConfig: aks.listClusterAdminCredential().kubeconfigs[0].value
    kubernetesNamespace: kubernetesNamespace
  }
}

module monitoring './azure-resources.bicep' = {
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
