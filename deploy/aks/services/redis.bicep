// Based on MIT-licensed https://github.com/Azure/azure-quickstart-templates/blob/master/quickstarts/microsoft.cache/redis-cache/main.bicep
// Updated to default to chepest tier.

@description('Used to create a unique name for this redis instance')
param solutionName string

@description('Specify the name of the Azure Redis Cache to create.')
param redisCacheName string = '${solutionName}-redis'

@description('Location of all resources')
param location string = resourceGroup().location

@description('Specify the pricing tier of the new Azure Redis Cache.')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param redisCacheSKU string = 'Basic'

@description('Specify the family for the sku. C = Basic/Standard, P = Premium.')
@allowed([
  'C'
  'P'
])
param redisCacheFamily string = 'C'

@description('Specify the size of the new Azure Redis Cache instance. Valid values: for C (Basic/Standard) family (0, 1, 2, 3, 4, 5, 6), for P (Premium) family (1, 2, 3, 4)')
@allowed([
  0
  1
  2
  3
  4
  5
  6
])
param redisCacheCapacity int = 0

@description('Specify a boolean value that indicates whether to allow access via non-SSL ports.')
param enableNonSslPort bool = false

resource redisCache 'Microsoft.Cache/Redis@2020-06-01' = {
  name: redisCacheName
  location: location
  properties: {
    enableNonSslPort: enableNonSslPort
    minimumTlsVersion: '1.2'
    sku: {
      capacity: redisCacheCapacity
      family: redisCacheFamily
      name: redisCacheSKU
    }
  }
}

//
// Diagnostics and Insights settings
//
//
// We are keeping diagnostics code here commented out in case we decide to re-enable it but
// for the time being it is broken due to https://github.com/Azure/azure-quickstart-templates/issues/13566
//

// @description('Specify a boolean value that indicates whether diagnostics should be saved to the specified storage account. Requires existingDiagnosticsStorageAccountName and existingDiagnosticsStorageAccountResourceGroup if set.')
// param diagnosticsEnabled bool = false

// @description('Specify the name of an existing storage account for diagnostics.')
// param existingDiagnosticsStorageAccountName string

// @description('Specify the resource group name of an existing storage account for diagnostics.')
// param existingDiagnosticsStorageAccountResourceGroup string


// resource diagnosticsStorage 'Microsoft.Storage/storageAccounts@2021-09-01' existing = {
//   scope: resourceGroup(existingDiagnosticsStorageAccountResourceGroup)
//   name: existingDiagnosticsStorageAccountName
// }

// resource Microsoft_Insights_diagnosticsettings_redisCacheName 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (diagnosticsEnabled) {
//   scope: redisCache
//   name: redisCache.name
//   properties: {
//     storageAccountId: diagnosticsStorage.id
//     metrics: [
//       {
//         timeGrain: 'AllMetrics'
//         enabled: diagnosticsEnabled
//         retentionPolicy: {
//           days: 90
//           enabled: diagnosticsEnabled
//         }
//       }
//     ]
//   }
// }

var redisPort = enableNonSslPort ? '6379' : '6380'

output redisHostnameAndPort string = '${redisCache.properties.hostName}:${redisPort}'
output redisPassword string = redisCache.listKeys().primaryKey
output redisEnableTLS bool = !enableNonSslPort

