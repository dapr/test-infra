# ------------------------------------------------------------
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
# ------------------------------------------------------------

import requests
import json
from global_settings import GITHUB_API_PARAMETER
from failure_log_crawler import FailureLogCrawler


class WorkFlowScaner:
    headres = {}
    runs_len = 0
    in_progress_num = 0
    success_num = 0
    failure_num = 0
    failure_id = []

    def __init__(self, repo, workflow_name, access_token):
        self.repo = repo
        self.workflow_name = workflow_name
        self.access_token = access_token
        self.headers = {
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": "2022-11-28",
            "Authorization": f"token {access_token}"
        }
        self.crawler = FailureLogCrawler(repo, access_token)

    def scan_workflow(self):
        url = f"https://api.github.com/repos/{self.repo}/actions/workflows/{self.workflow_name}/runs"
        response = requests.get(url, headers=self.headers, params=GITHUB_API_PARAMETER)
        runs = json.loads(response.text)["workflow_runs"]

        # ingore workflows triggered manually
        scheduled_runs = [r for r in runs if r["event"] == "schedule"]
        self.runs_len = len(scheduled_runs)
        print(f"Successfully get {self.runs_len} scheduled runs")

        for run in scheduled_runs:
            if run["status"] == "in_progress":
                self.in_progress_num += 1
            else:
                if run["conclusion"] == "success":
                    self.success_num += 1
                elif run["conclusion"] == "failure":
                    self.failure_num += 1
                    self.failure_id.append(run["id"])

        print(
            f"{self.in_progress_num} runs are still in progress, {self.success_num} runs success and {self.failure_num} runs fail"
        )

    def get_pass_rate(self):
        pass_rate = self.success_num / self.runs_len
        return pass_rate

    def list_failure_case(self):
        self.crawler.crawl(self.failure_id, self.runs_len)
        self.crawler.list_failure_testcase()
