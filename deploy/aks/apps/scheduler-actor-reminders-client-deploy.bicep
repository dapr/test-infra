@secure()
param kubeConfig string
param kubernetesNamespace string

extension kubernetes with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource coreService_schedulerActorRemindersClient 'core/Service@v1' = {
  metadata: {
    name: 'scheduler-actor-reminders-client'
    labels: {
      app: 'scheduler-actor-reminders-client'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    selector: {
      app: 'scheduler-actor-reminders-client'
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

resource appsDeployment_schedulerActorRemindersClient 'apps/Deployment@v1' = {
  metadata: {
    name: 'scheduler-actor-reminders-client-app'
    labels: {
      app: 'scheduler-actor-reminders-client'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'scheduler-actor-reminders-client'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'scheduler-actor-reminders-client'
        }
        annotations: {
          'dapr.io/enabled': 'true'
          'dapr.io/app-id': 'scheduler-actor-reminders-client'
          'dapr.io/enable-profiling': 'true'
          'dapr.io/log-as-json': 'true'
          'prometheus.io/scrape': 'true'
          'prometheus.io/port': '9988'
        }
      }
      spec: {
        containers: [
          {
            name: 'scheduler-actor-reminders-client'
            image: 'daprtests.azurecr.io/scheduler-actor-reminders-client:dev'
            ports: [
              {
                containerPort: 3008
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
