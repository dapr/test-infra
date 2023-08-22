@secure()
param kubeConfig string
param kubernetesNamespace string
param cosmosUrl string
param cosmosDatabaseName string
param cosmosContainerName string
param cosmosAccountPrimaryMasterKey string

import 'kubernetes@1.0.0' with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource daprIoComponentStatestore 'dapr.io/Component@v1alpha1' = {
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
