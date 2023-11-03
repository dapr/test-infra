param grafanaName string
param workspaceAzureMonitorName string
param workspaceLogAnalyticsName string
param location string
param grafanaAdminObjectId string
@description('ObjectID for an user in AAD you want to grant grafana admin rights. Default is to not provide anything: not grant this permission any individual')
param userGrafanaAdminObjectId string = ''
param dceName string
param dcrName string
param clusterName string

resource workspaceLogAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: workspaceLogAnalyticsName
  location: location
}

resource workspaceAzureMonitor 'microsoft.monitor/accounts@2021-06-03-preview' = {
  name: workspaceAzureMonitorName
  location: location
}

var azureMonitorWorkspaceResourceId = '/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/microsoft.monitor/accounts/${workspaceAzureMonitorName}'

resource grafana 'Microsoft.Dashboard/grafana@2022-08-01' = {
  name: grafanaName
  location: location
  properties: {
    grafanaIntegrations: {
      azureMonitorWorkspaceIntegrations: [
        {
          azureMonitorWorkspaceResourceId: azureMonitorWorkspaceResourceId
        }
      ]
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Standard'
  }
  dependsOn: [
    workspaceAzureMonitor
  ]
}

resource grafanaRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: '22926164-76b3-42b3-bc55-97df8dab3e41'
  scope: subscription()
}

resource amwRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  name: 'b0d8363b-8ddd-447d-831f-62ca05bff136'
  scope: subscription()
}

// Add user's as Grafana Admin for the Grafana instance
resource managedRoleAssignmentGrafana 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, grafanaAdminObjectId, 'grafana_role')
  scope: grafana
  properties: {
    roleDefinitionId: grafanaRole.id
    principalId: grafanaAdminObjectId
  }
}

resource userRoleAssignmentGrafana 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (userGrafanaAdminObjectId != '') {
  name: guid(resourceGroup().id, userGrafanaAdminObjectId, 'grafana_role')
  scope: grafana
  properties: {
    roleDefinitionId: grafanaRole.id
    principalId: userGrafanaAdminObjectId
  }
}

// Provide Grafana access to the AMW instance
resource roleAssignmentLocal 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, grafanaAdminObjectId, 'amw_role')
  properties: {
    roleDefinitionId: amwRole.id
    principalId: grafana.identity.principalId
  }
}
