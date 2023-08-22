param mediaPubsubComponentName string = 'receivemediapost'
param pubsubWorkflowComponentName string = 'longhaul-sb'
param environmentName string
param servicebusNamespace string

resource environment 'Microsoft.App/managedEnvironments@2022-03-01' existing = {
  name: environmentName
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2021-11-01' existing = {
  name: servicebusNamespace
}

var connectionString = listKeys('${serviceBus.id}/AuthorizationRules/RootManageSharedAccessKey', serviceBus.apiVersion).primaryConnectionString

resource pubsubWorkflowComponent 'Microsoft.App/managedEnvironments/daprComponents@2022-03-01' = {
  name: pubsubWorkflowComponentName
  parent: environment
  properties: {
    componentType: 'pubsub.azure.servicebus'
    version: 'v1'
    metadata: [
      {
        name: 'connectionString'
        secretRef: 'sb-root-connectionstring'
      }
    ]
    secrets: [
      {
        name: 'sb-root-connectionstring'
        value: connectionString
      }
    ]
    scopes: [
      'pubsub-workflow'
    ]
  }
}

resource mediaPubsubComponent 'Microsoft.App/managedEnvironments/daprComponents@2022-03-01' = {
  name: mediaPubsubComponentName
  parent: environment
  properties: {
    componentType: 'pubsub.azure.servicebus'
    version: 'v1'
    metadata: [
      {
        name: 'connectionString'
        secretRef: 'sb-root-connectionstring'
      }
    ]
    secrets: [
      {
        name: 'sb-root-connectionstring'
        value: connectionString
      }
    ]
    scopes: [
      'feed-generator'
      'message-analyzer'
    ]
  }
}
