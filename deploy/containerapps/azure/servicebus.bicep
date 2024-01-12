param serviceBusNamespace string = toLower('servicebus${uniqueString(resourceGroup().id)}')
param location string

resource servicebus 'Microsoft.ServiceBus/namespaces@2021-11-01' = {
  name: serviceBusNamespace
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 1
  }
}

output servicebusNamespace string = serviceBusNamespace
