@secure()
param kubeConfig string
param kubernetesNamespace string

@secure()
param connectionString string

import 'kubernetes@1.0.0' with {
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
