@secure()
param kubeConfig string
param kubernetesNamespace string

extension kubernetes with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource coreService_pubsubWorkflow 'core/Service@v1' = {
  metadata: {
    name: 'pubsub-workflow'
    labels: {
      app: 'pubsub-workflow'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    selector: {
      app: 'pubsub-workflow'
    }
    ports: [
      {
        protocol: 'TCP'
        port: 80
        targetPort: 9988
      }
    ]
    type: 'LoadBalancer'
  }
}

resource appsDeployment_pubsubWorkflowApp 'apps/Deployment@v1' = {
  metadata: {
    name: 'pubsub-workflow-app'
    labels: {
      app: 'pubsub-workflow'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'pubsub-workflow'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'pubsub-workflow'
        }
        annotations: {
          'dapr.io/enabled': 'true'
          'dapr.io/app-id': 'pubsub-workflow'
          'dapr.io/app-port': '3000'
          'dapr.io/enable-profiling': 'true'
          'dapr.io/log-as-json': 'true'
          'prometheus.io/scrape': 'true'
          'prometheus.io/port': '9988'
        }
      }
      spec: {
        containers: [
          {
            name: 'pubsub-workflow'
            image: 'daprtests.azurecr.io/pubsub-workflow:dev'
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
