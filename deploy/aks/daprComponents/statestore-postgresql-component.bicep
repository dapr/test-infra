@secure()
param kubeConfig string
param kubernetesNamespace string

@secure()
param connectionString string

extension kubernetes with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource daprIoComponentStatestore 'dapr.io/Component@v1alpha1' = {
  metadata: {
    name: 'statestore'
    namespace: kubernetesNamespace
  }
  spec: {
    type: 'state.postgresql'
    version: 'v1'
    metadata: [
      {
        name: 'connectionString'
        value: connectionString
      }
      {
        name: 'actorStateStore'
        value: 'true'
      }
    ]
  }
}
