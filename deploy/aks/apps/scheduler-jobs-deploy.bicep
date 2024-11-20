@secure()
param kubeConfig string
param kubernetesNamespace string

import 'kubernetes@1.0.0' with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource coreService_schedulerJobs 'core/Service@v1' = {
  metadata: {
    name: 'scheduler-jobs'
    labels: {
      app: 'scheduler-jobs'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    selector: {
      app: 'scheduler-jobs'
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

resource appsDeployment_schedulerJobs 'apps/Deployment@v1' = {
  metadata: {
    name: 'scheduler-jobs-app'
    labels: {
      app: 'scheduler-jobs'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'scheduler-jobs'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'scheduler-jobs'
        }
        annotations: {
          'dapr.io/enabled': 'true'
          'dapr.io/app-id': 'scheduler-jobs'
          'dapr.io/enable-profiling': 'true'
          'dapr.io/log-as-json': 'true'
          'prometheus.io/scrape': 'true'
          'prometheus.io/port': '9988'
        }
      }
      spec: {
        containers: [
          {
            name: 'scheduler-jobs'
            image: 'daprtests.azurecr.io/scheduler-jobs:dev'
            ports: [
              {
                containerPort: 3006
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
