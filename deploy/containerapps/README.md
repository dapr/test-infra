## Longhaul Test Suite on Container Apps
The longhaul applications can now be deployed in a Container Apps environment via an automated deployment. In order to deploy the applications to Container Apps, just run the following commands using the Azure CLI:

### Setup a Resource Group
```bash
export LOCATION='<region_of_choice>'
export RESOURCE_GROUP='<resource_group_name>'

az group create -n $RESOURCE_GROUP -l $LOCATION
```

### Deploy via Deployment Groups and Bicep
```bash
az deployment group create -g $RESOURCE_GROUP -f ./deploy/containerapps/main.bicep
```