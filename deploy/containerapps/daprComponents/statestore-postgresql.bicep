
param environmentName string

@secure()
param connectionString string

param componentName string = 'statestore'

resource environment 'Microsoft.App/managedEnvironments@2022-03-01' existing = {
  name: environmentName
}

resource stateDaprComponent 'Microsoft.App/managedEnvironments/daprComponents@2022-03-01' = {
  name: componentName
  parent: environment
  properties: {
    componentType: 'state.postgresql'
    version: 'v1'
    secrets: [
      {
        name: 'secretconnectionstring'
        value: connectionString
      }
    ]
    metadata: [
      {
        name: 'connectionString'
        secretRef: 'secretconnectionstring'
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
