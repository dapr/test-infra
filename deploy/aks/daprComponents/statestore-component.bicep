@secure()
param kubeConfig string
param kubernetesNamespace string

@secure()
param redisHostnameAndPort string

@secure()
param redisPassword string

param redisEnableTLS bool

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
    type: 'state.redis'
    version: 'v1'
    metadata: [
      {
        name: 'enableTLS'
        value: redisEnableTLS ? 'true' : 'false'
      }
      {
        name: 'redisHost'
        value: redisHostnameAndPort
      }
      {
        name: 'redisPassword'
        value: redisPassword
      }
      {
        name: 'actorStateStore'
        value: 'true'
      }
    ]
  }
}
