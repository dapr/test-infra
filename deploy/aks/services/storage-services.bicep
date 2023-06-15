@secure()
param kubeConfig string
param kubernetesNamespace string
param location string
param accountName string
param principalId string
param queueName string

import 'kubernetes@1.0.0' with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

// Storage account and associated services.
resource storageAccount 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: accountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

resource storagequeue 'Microsoft.Storage/storageAccounts/queueServices@2021-09-01' = {
  name: 'default'
  parent: storageAccount
}

resource blobstore 'Microsoft.Storage/storageAccounts/blobServices@2021-09-01' = {
  name: 'default'
  parent: storageAccount
}

resource storageServiceContributorRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: '17d1049b-9a84-46fb-8f53-869881c3d3ab' // GUID for storage account contributor.
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, principalId, 'storage_role')
  properties: {
    roleDefinitionId: storageServiceContributorRoleDefinition.id
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}

resource daprIoComponent_messagebinding 'dapr.io/Component@v1alpha1' = {
  metadata: {
    name: 'messagebinding'
    namespace: kubernetesNamespace
  }
  spec: {
    type: 'bindings.azure.storagequeues'
    version: 'v1'
    metadata: [
      {
        name: 'storageAccount'
        value: accountName
      }
      {
        name: 'accountKey'
        value: storageAccount.listKeys(storageAccount.apiVersion).keys[0].value
      }
      {
        name: 'queue'
        value: queueName
      }
    ]
  }
}

output location string = location
output accountName string = accountName
