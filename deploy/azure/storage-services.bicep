param location string
param accountName string = 'testlonghaulstorage'


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

output location string = location
output accountName string = accountName
