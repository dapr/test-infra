@secure()
param kubeConfig string
param kubernetesNamespace string

@secure()
param serviceBusConnectionString string

import 'kubernetes@1.0.0' with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource daprIoComponent_longhaulSbRapid 'dapr.io/Component@v1alpha1' = {
  metadata: {
    name: 'longhaul-sb-rapid'
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
}

resource daprIoComponent_longhaulSbMedium 'dapr.io/Component@v1alpha1' = {
  metadata: {
    name: 'longhaul-sb-medium'
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
}

resource daprIoComponent_longhaulSbSlow 'dapr.io/Component@v1alpha1' = {
  metadata: {
    name: 'longhaul-sb-slow'
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
}

resource daprIoComponent_longhaulSbGlacial 'dapr.io/Component@v1alpha1' = {
  metadata: {
    name: 'longhaul-sb-glacial'
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
}
