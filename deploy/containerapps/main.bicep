// Global parameters
param location string = resourceGroup().location

// === Azure Setup ===
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

// CosmosDB
module cosmos 'azure/cosmos.bicep' = {
  name: '${deployment().name}--cosmos'
  params: {
    location: location
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
module statestore 'daprComponents/statestore.bicep' = {
  name: '${deployment().name}--statestore-component'
  dependsOn: [
    cosmos
    environment
  ]
  params: {
    environmentName: environment.outputs.environmentName
    cosmosAccountName: cosmos.outputs.cosmosAccountName
    databaseName: cosmos.outputs.cosmosDatabaseName
    collectionName: cosmos.outputs.cosmosContainerName
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
    environment
    servicebus
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
    environment
    servicebus
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
    environment
    cosmos
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
    environment
    storageServices
    binding
  ]
  params: {
    environmentName: environment.outputs.environmentName
    location: location
  }
}

module messageAnalyzer 'apps/message-analyzer.bicep' = {
  name: '${deployment().name}--message-analyzer'
  dependsOn: [
    environment
    servicebus
    storageServices
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
    environment
    cosmos
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
    environment
    snapshot
  ]
  params: {
    environmentName: environment.outputs.environmentName
    location: location
  }
}
