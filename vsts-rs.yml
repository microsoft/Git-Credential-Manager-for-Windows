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
# == RealSign and Release Pipeline ==
# 
# Learn more at https://docs.microsoft.com/en-us/vsts/pipelines/get-started-yaml?view=vsts&tabs=gitvsts

phases:
  - template: vsts-tt.yml
    parameters:
      QueueName: 'VSEng-MicroBuildVS2017'

      CoreBuild:
        - task: MSBuild@1
          displayName: 'Core Build'
          inputs:
            solution: 'Installer/Installer.proj'
            msbuildArguments: '/nologo /verbosity:$(Build.Verbosity) "/binaryLogger:$(Build.SourcesDirectory)/$(build.buildNumber).binlog"'
            platform: '$(Build.Platform)'
            configuration: '$(Build.Configuration)'
            maximumCpuCount: false
            msbuildArchitecture: '$(Build.Architecture)'

      MicrobuildSetup:
        - task: ms-vseng.MicroBuildTasks.30666190-6959-11e5-9f96-f56098202fef.MicroBuildSigningPlugin@1
          displayName: Setup MicroBuild Signing
          condition: and(succeeded(), or(eq(variables['signType'],'real'), eq(variables['signType'],'test')))
          continueOnError: true
          inputs:
            feedSource: https://devdiv.pkgs.visualstudio.com/DefaultCollection/_packaging/MicroBuildToolset/nuget/v3/index.json
            signType: '$(SignType)'
            zipSources: true

      MicrobuildClean:
        - task: ms-vseng.MicroBuildTasks.521a94ea-9e68-468a-8167-6dcf361ea776.MicroBuildCleanup@1
          displayName: MicroBuild Cleanup
          condition: or(eq(variables['signType'],'real'), eq(variables['signType'],'test'))
          continueOnError: true

      NugetCommand:
        - task: NuGetCommand@2
          displayName: NuGet restore
          inputs:
            restoreSolution: 'GitCredentialManager.sln'
            verbosityRestore: 'quiet'
        
        - task: NuGetCommand@2
          displayName: NuGet restore
          inputs:
            command: custom
            arguments: restore "$(Build.SourcesDirectory)\Installer\packages.config" -PackagesDirectory "$(Build.SourcesDirectory)\packages" -SolutionDirectory "$(Build.SourcesDirectory)" -Verbosity Quiet

      PublishArtifacts:
        - task: PublishBuildArtifacts@1
          displayName: 'Publish Artifact: Binary Log'
          continueOnError: true
          condition:  eq(variables['system.pullrequest.isfork'], false)
          inputs:
            pathtoPublish: $(Build.SourcesDirectory)\$(build.buildNumber).binlog
            artifactName: $(build.buildNumber).binlog
            publishLocation: Container
        
        - task: PublishBuildArtifacts@1
          displayName: 'Publish Artifact: Binaries'
          continueOnError: true
          condition: and(succeeded(), eq(variables['system.pullrequest.isfork'], false))
          inputs:
            pathtoPublish: $(Build.SourcesDirectory)\Deploy\
            artifactName: $(Build.BuildNumber).output
            publishLocation: Container
