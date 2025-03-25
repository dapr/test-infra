@secure()
param kubeConfig string
param kubernetesNamespace string

extension kubernetes with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource coreService_schedulerWorkflow 'core/Service@v1' = {
  metadata: {
    name: 'scheduler-workflow'
    labels: {
      app: 'scheduler-workflow'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    selector: {
      app: 'scheduler-workflow'
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

resource appsDeployment_schedulerWorkflow 'apps/Deployment@v1' = {
  metadata: {
    name: 'scheduler-workflow-app'
    labels: {
      app: 'scheduler-workflow'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'scheduler-workflow'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'scheduler-workflow'
        }
        annotations: {
          'dapr.io/enabled': 'true'
          'dapr.io/app-id': 'scheduler-workflow'
          'dapr.io/enable-profiling': 'true'
          'dapr.io/log-as-json': 'true'
          'prometheus.io/scrape': 'true'
          'prometheus.io/port': '9988'
        }
      }
      spec: {
        containers: [
          {
            name: 'scheduler-workflow'
            image: 'daprtests.azurecr.io/scheduler-workflow:dev'
            ports: [
              {
                containerPort: 3009
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
