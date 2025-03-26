@secure()
param kubeConfig string
param kubernetesNamespace string

extension kubernetes with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource appsDeployment_hashtagActorApp 'apps/Deployment@v1' = {
  metadata: {
    name: 'hashtag-actor-app'
    labels: {
      app: 'hashtag-actor'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'hashtag-actor'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'hashtag-actor'
        }
        annotations: {
          'dapr.io/enabled': 'true'
          'dapr.io/app-id': 'hashtag-actor'
          'dapr.io/app-port': '3000'
          'dapr.io/enable-profiling': 'true'
          'dapr.io/log-as-json': 'true'
        }
      }
      spec: {
        containers: [
          {
            name: 'hashtag-actor'
            image: 'daprtests.azurecr.io/hashtag-actor:dev'
            ports: [
              {
                containerPort: 3000
              }
            ]
            imagePullPolicy: 'Always'
          }
        ]
      }
    }
  }
}
