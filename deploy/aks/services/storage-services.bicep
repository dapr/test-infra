param location string
param accountName string
param principalId string


// Storage account and associated services.
resource storageAccount 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: accountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

var storageAccountKey = storageAccount.listKeys(storageAccount.apiVersion).keys[0].value


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
   // TODO(tmacam) What is this value?!
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


output storageAccountKey string = storageAccountKey
output storageAccountName string = storageAccount.name
output storageQueueName string = storagequeue.name
