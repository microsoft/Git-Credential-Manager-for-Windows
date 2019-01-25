---
name: Git failure due to authentication
about: Create a report to help us improve
title: ''
labels: ''
assignees: ''

---

**Which Version of GCM are you using ?**
From a command prompt, run ``git credential-manager version`` and paste the output.
<!-- Ex: 1.18.1, 1.18.2, etc.. -->

**Which service are you trying to connect to**
* [ ] Azure DevOps
* [ ] Azure DevOps Server (TFS/on-prem)
* [ ] GitHub
* [ ] GitHub Enterprise
* [ ] Bitbucket
* [ ] Other? - please describe;


**If AzureDevops Hosted describe your url**
* [ ] dev.azure.com/org
* [ ] org.visualstudio.com

**If you're using Azure DevOps, can you access the repository in the browser via the same URL?**
* [ ] Yes
* [ ] No, I get a permission error.
* [ ] No, for a different reason: 

**If you're using Azure DevOps, and the account picker shows more than one identity as you authenticate, check that you selected the same one that has access on the web.**
* [ ] I only see one identity.
* [ ] I checked each and none worked.

**Expected behavior**
A clear and concise description of what you expected to happen (or code).

**Actual behavior**
A clear and concise description of what happens, e.g. exception is thrown, UI freezes  

**Set the env variables GCM_TRACE=1 and GIT_TRACE=1 and run your git command.  Redact any private information and attach the log**
