# Build Agents and Automation

Build agents, and other automation, often require specialized setup and configuration.
While there is detailed documentation on [GCM configuration options](Configuration.md), below are common recommendations for settings agents often require to operate.

_Note:_ SSH is often a better choice for automated system because requiring interactivity is a non-default option, and SSH is known to be secure and reliable.

## Recommendations for Azure DevOps Build Services

The majority of build definitions will work with a single repository, or at least a set of repositories which all have the same authentication requirements.
In this case, it is generally better to rely on [Azure DevOps Build Variables](https://docs.microsoft.com/en-us/vsts/build-release/concepts/definitions/build/variables?tabs=batch); specifically the `$(System.AccessToken)` build process OAuth token.
To enable scripts to use the build process OAuth token, go to the `Options` tab of the build definition and select `Allow Scripts to Access OAuth Token`.
For more information, read [Azure DevOps: Use the OAuth token to access the REST API](https://docs.microsoft.com/en-us/vsts/build-release/actions/scripts/powershell#oauth).

## Recommendations for Other Build Services

Build agents cannot manage modal dialogs, therefore we recommended the following configuration.

```shell
git config --global credential.interactive never
```

Build agents often need to minimize the amount of network traffic they generate.

To avoid Microsoft Account vs. Azure Active Directory look-up against an Azure DevOps account use...

... for Azure Directory backed authentication:

```shell
git config --global credential.authority Azure
```

... for Microsoft Account backed authentication:

```shell
git config --global credential.authority Microsoft
```

... to restrict the lifetime of VSTS personal access tokens:

```shell
git config --global credential.tokenDuration 1
```

If your agents rely on an on premise instance of Team Foundation Server and [Windows Domain Authentication](https://msdn.microsoft.com/en-us/library/ee253152(v=bts.10).aspx), use:

```shell
git config --global credential.authority NTLM
```

To avoid unnecessary service account credential validation, when relying on Microsoft Account or Azure Active Directory use:

```shell
git config --global credential.validate false
```
