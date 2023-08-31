# ------------------------------------------------------------
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
# ------------------------------------------------------------

from global_settings import *
from workflow_scan import WorkFlowScaner


if __name__ == "__main__":
    work_flow_scaner = WorkFlowScaner(REPO, WORKFLOW_NAME, ACCSS_TOKEM)
    print(
        f"Dapr E2E Tests Crawler start. \nREPO : {REPO}  WORKFLOW_NAME : {WORKFLOW_NAME}"
    )
    work_flow_scaner.scan_workflow()
    
    pass_rate_string = f"Pass rate of {WORKFLOW_NAME} is "+ "{:.2%}\n".format(work_flow_scaner.get_pass_rate())
    print("\n")
    print(pass_rate_string)
    
    file = open(OUTPUT_TARGET, 'w')  
    file.write(pass_rate_string + "\n")   
    file.close()  

    print("\n")
    print("Failure Workflow crawling start:")
    work_flow_scaner.list_failure_case()
 