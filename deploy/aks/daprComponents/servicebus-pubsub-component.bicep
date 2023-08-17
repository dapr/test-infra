@secure()
param kubeConfig string
param kubernetesNamespace string

@secure()
param serviceBusConnectionString string

import 'kubernetes@1.0.0' with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

param allPubSubTopicNames array = [
  'receivemediapost'
  'longhaul-sb-rapid'
  'longhaul-sb-medium'
  'longhaul-sb-slow'
  'longhaul-sb-glacial'
]


// We need a few pubsubs for our applications. They have different purposes
// but are functionally the same. We can use a loop to create them all at once.
resource daprserviceBusPubSubComponents 'dapr.io/Component@v1alpha1' = [for pubsubName in allPubSubTopicNames: {
  metadata: {
    name: pubsubName
    namespace: kubernetesNamespace
  }
  spec: {
    type: 'pubsub.azure.servicebus'
    version: 'v1'
    metadata: [
      {
        name: 'connectionString'
        value: serviceBusConnectionString
      }
    ]
  }
}]
