# Dapr Components Contrib Certification Tests Github Actions Workflow

## Overview

This sets up the [Workload Identity Federation](https://cloud.google.com/iam/docs/workload-identity-federation) in the Components Contrib Gith Actions Workflow
for the Conformance and Certification Tests.

The Terraform scripts follow steps similar to the suggested in the [Google Github Actions Auth](https://github.com/google-github-actions/auth#setting-up-workload-identity-federation)

The Terraform state is stored in [dapr-compoments-contrib-cert-tests](https://console.cloud.google.com/storage/browser/dapr-compoments-contrib-cert-tests?project=dapr-tests) Bucket
of the GCP GCS within the `dapr-tests` GCP Project.


## Inputs

| Name | Description | Type | Default | Required |
|------|-------------|------|---------|:--------:|
| project\_id | The project id that hosts the WIF pool and Dapr OSS SA | `string` | n/a | yes |
| gh\_repo    | The Github Repo (username/repo_name) to associate with the WIF pool and Dapr SA | `string` | n/a | yes |
| service_account | The Dapr OSS SA used for Github WIF OIDC | `string` | n/a | yes |
| wif\_pool\_name | The Dapr OSS Workload Identity Pool Name | `string` | n/a | yes |

```
$ terraform init

$ terraform plan -var="gh_repo=dapr/components-contrib" \
                 -var="project_id=dapr-tests" -var="service_account=comp-contrib-wif" \
                 -var="wif_pool_name=contrib-cert-tests"

$ terraform apply --auto-approve -var="gh_repo=dapr/components-contrib" \
                 -var="project_id=dapr-tests" -var="service_account=comp-contrib-wif" \
                 -var="wif_pool_name=contrib-cert-tests"
```


## Outputs
```
$ terraform output                                                   
    
pool_name = "projects/369878874207/locations/global/workloadIdentityPools/contrib-cert-tests-gh-pool"
provider_name = "projects/369878874207/locations/global/workloadIdentityPools/contrib-cert-tests-gh-pool/providers/contrib-cert-tests-gh-provider"
sa_email = "comp-contrib-wif@dapr-tests.iam.gserviceaccount.com"
```
