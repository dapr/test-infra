@secure()
param kubeConfig string
param kubernetesNamespace string
@secure()
param storageAccountKey string
param storageAccountName string
param storageQueueName string


extension kubernetes with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource daprIoComponentMessageBinding 'dapr.io/Component@v1alpha1' = {
  metadata: {
    name: 'messagebinding'
    namespace: kubernetesNamespace
  }
  spec: {
    type: 'bindings.azure.storagequeues'
    version: 'v1'
    metadata: [
      {
        name: 'storageAccount'
        value: storageAccountName
      }
      {
        name: 'accountKey'
        value: storageAccountKey
      }
      {
        name: 'queue'
        value: storageQueueName
      }
    ]
  }
}
