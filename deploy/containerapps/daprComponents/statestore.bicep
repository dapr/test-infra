param componentName string = 'statestore-cosmos'
param environmentName string
param databaseName string
param collectionName string
@secure()
param cosmosAccountName string

resource environment 'Microsoft.App/managedEnvironments@2022-03-01' existing = {
  name: environmentName
}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2021-10-15' existing = {
  name: cosmosAccountName
}

resource stateDaprComponent 'Microsoft.App/managedEnvironments/daprComponents@2022-03-01' = {
  name: componentName
  parent: environment
  properties: {
    componentType: 'state.azure.cosmosdb'
    version: 'v1'
    secrets: [
      {
        name: 'url'
        value: 'https://${cosmosAccountName}.documents.azure.com:443/'
      }
      {
        name: 'master-key'
        value: listKeys(cosmosAccount.id, cosmosAccount.apiVersion).primaryMasterKey
      }
    ]
    metadata: [
      {
        name: 'database'
        value: databaseName
      }
      {
        name: 'collection'
        value: collectionName
      }
      {
        name: 'url'
        secretRef: 'url'
      }
      {
        name: 'masterKey'
        secretRef: 'master-key'
      }
      {
        name: 'actorStateStore'
        value: 'true'
      }
    ]
    scopes: [
      'hashtag-actor'
      'snapshot'
    ]
  }
}
