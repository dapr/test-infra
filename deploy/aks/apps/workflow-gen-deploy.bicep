@secure()
param kubeConfig string
param kubernetesNamespace string

import 'kubernetes@1.0.0' with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource coreService_workflowGen 'core/Service@v1' = {
  metadata: {
    name: 'workflow-gen'
    labels: {
      app: 'workflow-gen'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    selector: {
      app: 'workflow-gen'
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

resource appsDeployment_workflowGenApp 'apps/Deployment@v1' = {
  metadata: {
    name: 'workflow-gen-app'
    labels: {
      app: 'workflow-gen'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'workflow-gen'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'workflow-gen'
        }
        annotations: {
          'dapr.io/enabled': 'true'
          'dapr.io/app-id': 'workflow-gen'
          'dapr.io/log-as-json': 'true'
          'prometheus.io/scrape': 'true'
          'prometheus.io/port': '9988'
        }
      }
      spec: {
        containers: [
          {
            name: 'workflow-gen'
            image: 'daprtests.azurecr.io/workflow-gen:dev'
            ports: [
              // This app does NOT expose an HTTP application port (so no 80 or 3000 port mapping here)
              // This app exposes a Prometheus metrics endpoint on port 9988
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
