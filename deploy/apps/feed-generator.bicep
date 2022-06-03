param appName string = 'feed-generator'
param environmentName string
param location string

resource environment 'Microsoft.App/managedEnvironments@2022-03-01' existing = {
  name: environmentName
}

resource feedGenerator 'Microsoft.App/containerApps@2022-03-01' = {
  name: appName
  location: location
  properties: {
    managedEnvironmentId: environment.id
    template: {
      containers: [
        {
          name: appName
          image: 'dapriotest/${appName}:dev'
        }
      ]
      scale: {
        minReplicas:  1
        maxReplicas: 1
      }
    }
    configuration: {
      dapr: {
        enabled: true
        appId: appName
      }
    }
  }
}
