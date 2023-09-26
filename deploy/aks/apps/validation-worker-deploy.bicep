@secure()
param kubeConfig string
param kubernetesNamespace string

import 'kubernetes@1.0.0' with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource coreService_validationWorker 'core/Service@v1' = {
  metadata: {
    name: 'validation-worker'
    labels: {
      app: 'validation-worker'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    selector: {
      app: 'validation-worker'
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

resource appsDeployment_validationWorkerApp 'apps/Deployment@v1' = {
  metadata: {
    name: 'validation-worker-app'
    labels: {
      app: 'validation-worker'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'validation-worker'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'validation-worker'
        }
        annotations: {
          'dapr.io/enabled': 'true'
          'dapr.io/app-id': 'validation-worker'
          'dapr.io/enable-profiling': 'true'
          'dapr.io/log-as-json': 'true'
          'prometheus.io/scrape': 'true'
          'prometheus.io/port': '9988'
        }
      }
      spec: {
        containers: [
          {
            name: 'validation-worker'
            image: 'daprtests.azurecr.io/validation-worker:dev'
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
                name: 'DELAY_IN_SEC'
                value: '60'
              }
            ]
          }
        ]
      }
    }
  }
}
