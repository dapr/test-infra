@secure()
param kubeConfig string
param kubernetesNamespace string

import 'kubernetes@1.0.0' with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource coreService_hashtagCounter 'core/Service@v1' = {
  metadata: {
    name: 'hashtag-counter'
    labels: {
      app: 'hashtag-counter'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    selector: {
      app: 'hashtag-counter'
    }
    ports: [
      {
        protocol: 'TCP'
        port: 9988
        targetPort: 9988
      }
    ]
    type: 'ClusterIP'
  }
}

resource appsDeployment_hashtagCounterApp 'apps/Deployment@v1' = {
  metadata: {
    name: 'hashtag-counter-app'
    labels: {
      app: 'hashtag-counter'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'hashtag-counter'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'hashtag-counter'
        }
        annotations: {
          'dapr.io/enabled': 'true'
          'dapr.io/app-id': 'hashtag-counter'
          'dapr.io/app-port': '3000'
          'dapr.io/log-as-json': 'true'
          'prometheus.io/scrape': 'true'
          'prometheus.io/port': '9988'
        }
      }
      spec: {
        containers: [
          {
            name: 'hashtag-counter'
            image: 'daprtests.azurecr.io/hashtag-counter:dev'
            ports: [
              {
                containerPort: 3000
              }
              {
                containerPort: 9988
              }
            ]
            imagePullPolicy: 'Always'
          }
        ]
      }
    }
  }
}
