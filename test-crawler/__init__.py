# ------------------------------------------------------------
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
# ------------------------------------------------------------

from global_settings import REPO, WORKFLOW_NAME, ACCESS_TOKEN, OUTPUT_TARGET
from workflow_scan import WorkFlowScaner


if __name__ == "__main__":
    workflow_scaner = WorkFlowScaner(REPO, WORKFLOW_NAME, ACCESS_TOKEN)
    print(
        f"Dapr E2E Tests Crawler start. \nREPO : {REPO}  WORKFLOW_NAME : {WORKFLOW_NAME}"
    )
    workflow_scaner.scan_workflow()

    pass_rate_string = f"\nPass rate of {WORKFLOW_NAME} is " + "{:.2%}\n".format(
        workflow_scaner.get_pass_rate()
    )
    print(pass_rate_string)

    with open(OUTPUT_TARGET, "w") as file:
        file.write(pass_rate_string + "\n")

    print("\nFailure Workflow crawling start:")
    workflow_scaner.list_failure_case()
