@secure()
param kubeConfig string
param kubernetesNamespace string

extension kubernetes with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource longhaulNamespace 'core/Namespace@v1' = {
  metadata: {
    name: kubernetesNamespace
  }
}

@description('The name of the k8s namespace to use. This exists only to provide an implicit dependency')
output kubernetesNamespace string = longhaulNamespace.metadata.name
