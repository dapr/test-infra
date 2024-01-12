// Global parameters
param location string = resourceGroup().location

@description('The unique discriminator of the solution. This is used to ensure that resource names are unique.')
@minLength(3)
@maxLength(16)
param solutionName string = uniqueString(resourceGroup().id)

param identityName string = '${solutionName}-identity'

// === Azure Setup ===

// Identity - Not a module so we can reference the resource below.
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

// Container Apps Environment 
module environment 'azure/environment.bicep' = {
  name: '${deployment().name}--environment'
  params: {
    location: location
  }
}

// Blobstore
module storageServices 'azure/storage-services.bicep' = {
  name: '${deployment().name}--storage-services'
  params: {
    location: location
  }
}

// PostgreSQL
module postgresql '../aks/services/postgresql.bicep' = {
  name: '${deployment().name}--postgresql'
  params: {
    solutionName: solutionName
    location: location
    allowAzureIPsFirewall: true
    aadAdminName: managedIdentity.name
    aadAdminObjectid: managedIdentity.properties.principalId
  }
}

// Servicebus
module servicebus 'azure/servicebus.bicep' = {
  name: '${deployment().name}--servicebus'
  params: {
    location: location
  }
}

// === Component Setup ===
// Statestore (actors) component
module statestore 'daprComponents/statestore-postgresql.bicep' = {
  name: '${deployment().name}--statestore-component'
  dependsOn: [
    postgresql
    environment
  ]
  params: {
    environmentName: environment.outputs.environmentName
    connectionString: postgresql.outputs.connectionString
  }
}

// Servicebus pubsub component
module pubsub 'daprComponents/pubsub.bicep' = {
  name: '${deployment().name}--pubsub-component'
  dependsOn: [
    servicebus
    environment
  ]
  params: {
    environmentName: environment.outputs.environmentName
    servicebusNamespace: servicebus.outputs.servicebusNamespace
  }
}

// StorageQueue binding component
module binding 'daprComponents/storagebinding.bicep' = {
  name: '${deployment().name}--binding-component'
  dependsOn: [
    storageServices
    environment
  ]
  params: {
    environmentName: environment.outputs.environmentName
    storageAccountName: storageServices.outputs.accountName
  }
}

// === Application Setup ===
module pubsubWorkflow 'apps/pubsub-workflow.bicep' = {
  name: '${deployment().name}--pubsub-workflow'
  dependsOn: [
    pubsub
  ]
  params: {
    environmentName: environment.outputs.environmentName
    location: location
  }
}

module feedGenerator 'apps/feed-generator.bicep' = {
  name: '${deployment().name}--feed-generator'
  dependsOn: [
    pubsub
  ]
  params: {
    environmentName: environment.outputs.environmentName
    location: location
  }
}

module hashtagActor 'apps/hashtag-actor.bicep' = {
  name: '${deployment().name}--hashtag-actor'
  dependsOn: [
    statestore
  ]
  params: {
    environmentName: environment.outputs.environmentName
    location: location
  }
}

module hashtagCounter 'apps/hashtag-counter.bicep' = {
  name: '${deployment().name}--hashtag-counter'
  dependsOn: [
    binding
    hashtagActor
  ]
  params: {
    environmentName: environment.outputs.environmentName
    location: location
  }
}

module messageAnalyzer 'apps/message-analyzer.bicep' = {
  name: '${deployment().name}--message-analyzer'
  dependsOn: [
    binding
    pubsub
  ]
  params: {
    environmentName: environment.outputs.environmentName
    location: location
  }
}

module snapshot 'apps/snapshot.bicep' = {
  name: '${deployment().name}--snapshot'
  dependsOn: [
    statestore
  ]
  params: {
    environmentName: environment.outputs.environmentName
    location: location
  }
}

module validationWorker 'apps/validation-worker.bicep' = {
  name: '${deployment().name}--validation-worker'
  dependsOn: [
    snapshot
  ]
  params: {
    environmentName: environment.outputs.environmentName
    location: location
  }
}
