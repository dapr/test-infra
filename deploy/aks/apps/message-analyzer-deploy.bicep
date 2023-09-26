@secure()
param kubeConfig string
param kubernetesNamespace string

import 'kubernetes@1.0.0' with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource coreService_messageAnalyzer 'core/Service@v1' = {
  metadata: {
    name: 'message-analyzer'
    labels: {
      app: 'message-analyzer'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    selector: {
      app: 'message-analyzer'
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

resource appsDeployment_messageAnalyzerApp 'apps/Deployment@v1' = {
  metadata: {
    name: 'message-analyzer-app'
    labels: {
      app: 'message-analyzer'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'message-analyzer'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'message-analyzer'
        }
        annotations: {
          'dapr.io/enabled': 'true'
          'dapr.io/app-id': 'message-analyzer'
          'dapr.io/app-port': '80'
          'dapr.io/enable-profiling': 'true'
          'dapr.io/log-as-json': 'true'
          'prometheus.io/scrape': 'true'
          'prometheus.io/port': '9988'
        }
      }
      spec: {
        containers: [
          {
            name: 'message-analyzer'
            image: 'daprtests.azurecr.io/message-analyzer:dev'
            ports: [
              {
                containerPort: 80
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
