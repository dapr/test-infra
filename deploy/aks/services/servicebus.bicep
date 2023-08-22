param serviceBusNamespace string
param location string
param principalId string


resource servicebus 'Microsoft.ServiceBus/namespaces@2021-11-01' = {
  name: serviceBusNamespace
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 1
  }
}

resource serviceBusDataOwnerRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: '090c5cfd-751d-490a-894a-3ce6f1109419' // GUID for service bus data owner.
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, principalId, 'servicebus_role')
  properties: {
    roleDefinitionId: serviceBusDataOwnerRoleDefinition.id
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}


var serviceBusEndpoint = '${servicebus.id}/AuthorizationRules/RootManageSharedAccessKey'


output serviceBusConnectionString string = listKeys(serviceBusEndpoint, servicebus.apiVersion).primaryConnectionString
output serviceBusNamespace string = serviceBusNamespace
