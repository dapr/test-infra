@secure()
param kubeConfig string
param kubernetesNamespace string
param cosmosAccountName string
param cosmosUrl string
param cosmosDatabaseName string
param cosmosContainerName string
param cosmosAccountPrimaryMasterKey string

import 'kubernetes@1.0.0' with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

// resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' existing = {
//   name: cosmosAccountName
//   scope: resourceGroup()
// }

resource daprIoComponent_statestore 'dapr.io/Component@v1alpha1' = {
  metadata: {
    name: 'statestore'
    namespace: kubernetesNamespace
  }
  spec: {
    type: 'state.azure.cosmosdb'
    version: 'v1'
    metadata: [
      {
        name: 'url'
        value: cosmosUrl
      }
      {
        name: 'masterKey'
        value: cosmosAccountPrimaryMasterKey
      }
      {
        name: 'database'
        value: cosmosDatabaseName
      }
      {
        name: 'collection'
        value: cosmosContainerName
      }
      {
        name: 'actorStateStore'
        value: 'true'
      }
    ]
  }
}
