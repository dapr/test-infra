@secure()
param kubeConfig string
param kubernetesNamespace string

import 'kubernetes@1.0.0' with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource coreService_feedGenerator 'core/Service@v1' = {
  metadata: {
    name: 'feed-generator'
    labels: {
      app: 'feed-generator'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    selector: {
      app: 'feed-generator'
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

resource appsDeployment_feedGeneratorApp 'apps/Deployment@v1' = {
  metadata: {
    name: 'feed-generator-app'
    labels: {
      app: 'feed-generator'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'feed-generator'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'feed-generator'
        }
        annotations: {
          'dapr.io/enabled': 'true'
          'dapr.io/app-id': 'feed-generator'
          'dapr.io/log-as-json': 'true'
          'prometheus.io/scrape': 'true'
          'prometheus.io/port': '9988'
        }
      }
      spec: {
        containers: [
          {
            name: 'feed-generator'
            image: 'daprtests.azurecr.io/feed-generator:dev'
            ports: [
              {
                name: 'dapp'
                containerPort: 3000
              }
              {
                name: 'prom'
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
