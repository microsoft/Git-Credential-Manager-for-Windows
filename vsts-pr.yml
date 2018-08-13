# ** Git Credential Manager for Windows **
# 
# Copyright (c) Microsoft Corporation
# All rights reserved.
# 
# MIT License
# 
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the """"Software""""), to deal
# in the Software without restriction, including without limitation the rights to
# use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
# the Software, and to permit persons to whom the Software is furnished to do so,
# subject to the following conditions:
# 
# The above copyright notice and this permission notice shall be included in all
# copies or substantial portions of the Software.
# 
# THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
# FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
# COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
# AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
# WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
# 
# == Pull Request Validtion Pipeline ==
# 
# Learn more at https://docs.microsoft.com/en-us/vsts/pipelines/get-started-yaml?view=vsts&tabs=gitvsts
# https://microsoft-git-tools.visualstudio.com/Git-Credential-Manager-for-Windows

phases:
  - template: vsts-tt.yml
    parameters:
      QueueName: 'Hosted VS2017'

      CoreBuild:
        - task: MSBuild@1
          displayName: 'Core Build'
          inputs:
            solution: "GitCredentialManager.sln"
            msbuildArguments: /nologo /verbosity:$(Build.Verbosity) "/binaryLogger:$(Build.SourcesDirectory)/$(build.buildNumber).binlog"
            platform: '$(Build.Platform)'
            configuration: '$(Build.Configuration)'
            maximumCpuCount: false
            msbuildArchitecture: '$(Build.Architecture)'

      MicrobuildSetup: []
      MicrobuildClean: []

      NugetCommand:
        - task: NuGetCommand@2
          displayName: NuGet restore
          inputs:
            restoreSolution: 'GitCredentialManager.sln'
            verbosityRestore: 'quiet'

      PublishArtifacts: []
