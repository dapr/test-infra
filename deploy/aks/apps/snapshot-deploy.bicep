@secure()
param kubeConfig string
param kubernetesNamespace string

extension kubernetes with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource coreService_snapshot 'core/Service@v1' = {
  metadata: {
    name: 'snapshot'
    labels: {
      app: 'snapshot'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    selector: {
      app: 'snapshot'
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

resource appsDeployment_snapshotApp 'apps/Deployment@v1' = {
  metadata: {
    name: 'snapshot-app'
    labels: {
      app: 'snapshot'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'snapshot'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'snapshot'
        }
        annotations: {
          'dapr.io/enabled': 'true'
          'dapr.io/app-id': 'snapshot'
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
            name: 'snapshot'
            image: 'daprtests.azurecr.io/snapshot:dev'
            ports: [
              {
                containerPort: 3000
              }
              {
                containerPort: 9988
              }
            ]
            imagePullPolicy: 'Always'
            env: [
              {
                name: 'DELAY_IN_MS'
                value: '7000'
              }
            ]
          }
        ]
      }
    }
  }
}
