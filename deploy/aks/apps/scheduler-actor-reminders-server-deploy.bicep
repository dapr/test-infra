@secure()
param kubeConfig string
param kubernetesNamespace string

extension kubernetes with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource coreService_schedulerActorRemindersServer 'core/Service@v1' = {
  metadata: {
    name: 'scheduler-actor-reminders-server'
    labels: {
      app: 'scheduler-actor-reminders-server'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    selector: {
      app: 'scheduler-actor-reminders-server'
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

resource appsDeployment_schedulerActorRemindersServer 'apps/Deployment@v1' = {
  metadata: {
    name: 'scheduler-actor-reminders-server-app'
    labels: {
      app: 'scheduler-actor-reminders-server'
    }
    namespace: kubernetesNamespace
  }
  spec: {
    replicas: 1
    selector: {
      matchLabels: {
        app: 'scheduler-actor-reminders-server'
      }
    }
    template: {
      metadata: {
        labels: {
          app: 'scheduler-actor-reminders-server'
        }
        annotations: {
          'dapr.io/enabled': 'true'
          'dapr.io/app-id': 'scheduler-actor-reminders-server'
          'dapr.io/enable-profiling': 'true'
          'dapr.io/log-as-json': 'true'
          'prometheus.io/scrape': 'true'
          'prometheus.io/port': '9988'
        }
      }
      spec: {
        containers: [
          {
            name: 'scheduler-actor-reminders-server'
            image: 'daprtests.azurecr.io/scheduler-actor-reminders-server:dev'
            ports: [
              {
                containerPort: 3007
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
