@description('Used to create a unique name for this postgres instance')
param solutionName string

@description('Server Name for Azure Database for PostgreSQL Flexible Server instance')
param serverName string = '${solutionName}-postgres'

@description('Database administrator login name')
@minLength(1)
param administratorLogin string = 'pgadmin${uniqueString(resourceGroup().id)}'

@description('Database administrator password')
@minLength(8)
@secure()
param administratorLoginPassword string = 'pgpass-${uniqueString(resourceGroup().id)}'

@description('Azure database for PostgreSQL pricing tier')
@allowed([
  'Burstable'
  'Basic'
  'GeneralPurpose'
  'MemoryOptimized'
])
param skuTier string = 'Burstable'

@description('Azure database for PostgreSQL Flexible Server sku name ')
param skuName string = 'Standard_B1ms'

@description('Azure database for PostgreSQL Flexible Server Storage Size in GB ')
param storageSize int = 32

@description('PostgreSQL version')
@allowed([
  '11'
  '12'
  '13'
  '14'
  '15'
])
param postgresqlVersion string = '15'

@description('Location for all resources.')
param location string = resourceGroup().location

@description('PostgreSQL Flexible Server backup retention days')
param backupRetentionDays int = 7

@description('Geo-Redundant Backup setting')
@allowed([
  'Disabled'
  'Enabled'
])
param geoRedundantBackup string = 'Disabled'

@description('High Availability Mode')
@allowed([
  'Disabled'
  'ZoneRedundant'
  'SameZone'
])
param haMode string = 'Disabled'

@description('Active Directory Authetication')
@allowed([
  'Disabled'
  'Enabled'
])
param isActiveDirectoryAuthEnabled string = 'Enabled'

@description('PostgreSQL Authetication')
@allowed([
  'Disabled'
  'Enabled'
])
param isPostgreSQLAuthEnabled string = 'Enabled'

@description('The Object ID of the Azure AD admin.')
param aadAdminObjectid string

@description('Azure AD admin name.')
param aadAdminName string

@description('Azure AD admin Type')
@allowed([
  'User'
  'Group'
  'ServicePrincipal'
])
param aadAdminType string = 'ServicePrincipal'


param allowAzureIPsFirewall bool = false
param allowAllIPsFirewall bool = false

resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2022-12-01' = {
  name: serverName
  location: location
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    createMode: 'Default'
    version: postgresqlVersion
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    authConfig: {
      activeDirectoryAuth: isActiveDirectoryAuthEnabled
      passwordAuth: isPostgreSQLAuthEnabled
      tenantId: subscription().tenantId
    }
    storage: {
      storageSizeGB: storageSize
    }
    backup: {
      backupRetentionDays: backupRetentionDays
      geoRedundantBackup: geoRedundantBackup
    }
    highAvailability: {
      mode: haMode
    }
  }
}

// AAD support requires Dapr 1.12.0 or later
resource addAddUser 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2022-12-01' = {
  parent: server
  name: '${aadAdminObjectid}'
  dependsOn: [
    server
  ]
  properties: {
    tenantId: subscription().tenantId
    principalType: aadAdminType
    principalName: aadAdminName
  }
}

// Postgres examples nudge strongly towards using a private network to
// secure connectivity to the database. This is a good idea but seems
// too much for a demo and to mix with AKS.
//
// References:
// * https://github.com/Azure-Samples/dotNET-FrontEnd-to-BackEnd-on-Azure-Container-Apps/blob/2b85ffa3bf0359f3e89ee222375afa4f4fc6060a/infra/core/database/postgresql/flexibleserver.bicep#L46
// * https://github.com/Azure/ResourceModules/tree/50c54a404803d9f76f30dbcd8ab03797fabd78f7/modules/db-for-postgre-sql/flexible-server

resource firewall_all 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2022-12-01' = if (allowAllIPsFirewall) {
  name: 'allow-all-IPs'
  parent: server
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '255.255.255.255'
  }
}

resource firewall_azure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2022-12-01' = if (allowAzureIPsFirewall) {
  name: 'allow-all-azure-internal-IPs'
  parent: server
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}


var serverHostname  = server.properties.fullyQualifiedDomainName

var databaseName = 'postgres' // we could probably create our own but that's for next time

output connectionString string = 'host=${serverHostname} port=5432 database=${databaseName} user=${administratorLogin} password=${administratorLoginPassword} sslmode=require'
