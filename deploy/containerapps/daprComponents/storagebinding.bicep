param componentName string = 'messagebinding'
param queueName string = 'dapr-test'
param environmentName string
@secure()
param storageAccountName string

resource environment 'Microsoft.App/managedEnvironments@2022-03-01' existing = {
  name: environmentName
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-09-01' existing = {
  name: storageAccountName
}

resource storageQueueComponent 'Microsoft.App/managedEnvironments/daprComponents@2022-03-01' = {
  name: componentName
  parent: environment
  properties: {
    componentType: 'bindings.azure.storagequeues'
    version: 'v1'
    metadata: [
      {
        name: 'queue'
        value: queueName
      }
      {
        name: 'storageAccount'
        secretRef: 'storage-account'
      }
      {
        name: 'storageAccessKey'
        secretRef: 'storage-key'
      }
    ]
    secrets: [
      {
        name: 'storage-account'
        value: storageAccountName
      }
      {
        name: 'storage-key'
        value: listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value
      }
    ]
    scopes: [
      'hashtag-counter'
      'message-analyzer'
    ]
  }
}
