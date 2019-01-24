---
name: Git failure due to authentication
about: Create a report to help us improve
title: ''
labels: ''
assignees: ''

---

**Which Version of GCM are you using ?**
You can see the GCM version by setting the env variable GCM_TRACE=1 and performing a command.
<!-- Ex: 1.18.1, 1.18.2, etc.. -->

**Which service are you trying to connect to**
* Azure Devops
    * [ ] Hosted
    * [ ] On-Prem
* [ ] GitHub
* [ ] Bitbucket
* [ ] Other? - please describe;


**If AzureDevops Hosted describe your url**
* [ ] dev.azure.com/org
* [ ] org.visualstudio.com

**If AzureDevops, make sure verified that you can access the webpage of the remote url in browser.  Also double check that if you've been presented with an account picker you've selected the same one you used to access the webpage**
* [ ] Yes, I've done both of these things
* [ ] Other? - please describe;


**Expected behavior**
A clear and concise description of what you expected to happen (or code).

**Actual behavior**
A clear and concise description of what happens, e.g. exception is thrown, UI freezes  

**Set the env variables GCM_TRACE=1 and GIT_TRACE=1 and run your git command.  Redact any private information and attach the log**
