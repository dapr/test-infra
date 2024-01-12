param appName string = 'hashtag-counter'
param containerPort int = 3000
param environmentName string
param location string

resource environment 'Microsoft.App/managedEnvironments@2022-03-01' existing = {
  name: environmentName
}

resource hashtagCounter 'Microsoft.App/containerApps@2022-03-01' = {
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
      ingress: {
        external: false
        targetPort: containerPort
      }
      dapr: {
        enabled: true
        appId: appName
        appPort: containerPort
      }
    }
  }
}
