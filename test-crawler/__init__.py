# ------------------------------------------------------------
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
# ------------------------------------------------------------

from global_settings import REPO, WORKFLOW_NAME, ACCESS_TOKEN
from workflow_scan import WorkFlowScaner

if __name__ == "__main__":
    workflow_scaner = WorkFlowScaner(REPO, WORKFLOW_NAME, ACCESS_TOKEN)
    print(f"Dapr E2E Tests Crawler start. \nREPO : {REPO}  WORKFLOW_NAME : {WORKFLOW_NAME}")
    workflow_scaner.scan_workflow()

    print("Failure Workflow crawling start:")
    workflow_scaner.list_failure_case()
