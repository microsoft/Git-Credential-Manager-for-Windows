# Bitbucket Authentication, 2FA and OAuth

By default for authenticating against private Git repositories Bitbucket supports SSH and username/password Basic Auth over HTTPS. Username/password Basic Auth over HTTPS is also available for REST api access. Additionally Bitbucket supports App-specific passwords which can be used via Basic Auth as username/app-pecific-password.

To enhance security Bitbucket offers optional Two-Factor Authentication (2FA). When 2FA is enabled username/password Basic Auth access to the REST APIs and to Git repositories is suspended. At that point users are left with the choice of username/apps-pecific-password Basic Auth for REST APIs and Git interactions, OAuth for REST APIs and Git/Hg interactions or SSH for Git/HG interactions and one of the previous choices for REST APIs. SSH and REST API access are beyond the scope of this document. Follow this like to read about Bitbucket's 2FA implementation https://confluence.atlassian.com/bitbucket/two-step-verification-777023203.html

App-specific passwords are not particularly user friendly as once created Bitbucket hides their value, even from the owner. They are intended for use within application that talk to Bitbucket where application can remember and use the app-specific-password. For further information see https://confluence.atlassian.com/display/BITBUCKET/App+passwords

OAuth is the intended authentication method for user interactions with HTTPS remote urls for Git repositories when 2FA is active. essentially once a client application has an OAuth access token it can be used in place of a user's password. More information about Bitbucket's OAuth implementation can be found here https://confluence.atlassian.com/bitbucket/oauth-on-bitbucket-cloud-238027431.html

Bitbucket's OAuth implementation follows the standard specifications for OAuth 2.0, which is out of scope for this document. However it implements a comparitively rare part of OAuth 2.0 Refresh Tokens. Bitbucket's Access Token's expire after 1 hour if not revoked, as opposed to GitHub's that expire after 1 year. When GitHub's Access Tokens expire the user must articipate in the standard OAuth authentication flow to get a new Access Token. Since this occurs, in theory, once per year this is not too onerous. Since Bitbucket's Access Tokens expire every hour it is too much to expect a user to go through the OAuth authentication flow every hour. So Bitbcuket implements refresh Tokens. Refresh Tokens are issued to the client application at the same time as Access Tokens. They can only be used to request a new Access Token, and then only if they have not been revoked.

As such the support for Bitbucket and the use of its OAuth in the Git Credentials Manager differs significantly from GitHubs. this is explained in more detail below.

# Multiple user accounts.

Unlike the GitHub implementation within the Git Credential Manager, the Bitbucket implementation stores 'secrets', passwords, app-specific passwords, or oauth tokens, with usernames in the Windows Credential Manager/Vault.

Depending on the circumstances this means either saving an explicit username in to the Windows Credential Manager/Vault or including the username in the URL used as the identifying key of entries in the Windows Credential Manager/Vault, i.e. using a key such as 'git:https://mminns@bitbucket.org/ rather than 'git:https://bitbucket.org'.

This means that the Bitbucket implementation in the GCM can support multiple accounts, and usernames,  for a single user against Bitbucket, e.g. a personal account and a work account.

# Authentication User Experience.
When the GCM is triggered by Git, the GCM will check the 'host' parameter passed to it. If it contains 'bitbucket.org' it will trigger the Bitbucket related processes.

## Basic Authentication
If the GCM needs to prompt the user for credentials they will always be shown an initial dialog where they can enter a username and password. If the 'username' parameter was passed into the GCM it is used to pre-populate the username field, although it can be overriden.

When username and password credentials are submitted the GCM will use them to attempto retrieve a token, for Basic Auth this token is in effect the password the user just entered. The GCM retrieves this 'token' by checking the password can be used to successfully retrieve the User profile via the Bitbucket REST API. 

If the username/password credentials sent as Basic Auth credentials works, then the password is identified as the token. The credentials, the username and the password/token, are then stored and the values returned to Git.

If the request for the User profile via the REST API fails with a 401 return code it indicates the username/password combination is invalid, nothing is stored and nothing is returned to Git.

Hoever if the request fails with a 403 (Forbidden) return code, this indicates that the username and password are valid but 2FA is enabled on the Bitbucket Account. When this occurs the user it prompted to complete the OAuth authentication process.

## OAuth
Oauth authentication prompts the User with a new dialog where they can trigger  OAuth authentication. This involves opening a browser request to _https://bitbucket.org/site/oauth2/authorize?response_type=code&client_id={consumerkey}&state=authenticated&scope={scopes}&redirect_uri=http://localhost:34106/_ . This will trigger a flow on Bitbucket where the user must login, potentially including a 2FA prompt, and authorize the GCM to access Bitbucket with the specified scopes. The GCM will spawn a temporary, local webserver, listening on port 34106, to handle the OAuth redirect/callback. Assuming the user successfully logins into Bitbucket and authorizes the GCM this callback will include the Access and Refresh Tokens.

The Access and Refresh Tokens will be stored against the username and the username/Access Token credentials returned to Git.

# GCM concepts
## Approve
See Store

## Clear

Follows existing behaviour.

If a Bitbucket URL is identified an instance of the Bitbucket Authentication class is created and the Bitbucket Authentication.DeleteCredentials() method is called to stored the passed in credentials.

## Delete

Follows existing behaviour.

If a Bitbucket URL is identified an instance of the Bitbucket Authentication class is created and the Bitbucket Authentication.DeleteCredentials() method is called to stored the passed in credentials.

## Deploy

Not effected by adding Bitbucket support.

## Erase

Follows existing behaviour.

If a Bitbucket URL is identified an instance of the Bitbucket Authentication class is created and the Bitbucket Authentication.DeleteCredentials() method is called to stored the passed in credentials.

## Fill 

See Get

## Get

Follows existing behaviour.

If a Bitbucket URL is identified an instance of the Bitbucket Authentication class is created and is used to attempt to:
1. Get existing stored credentials using Authentication.GetGredentials()
2. If that fails it prompts for Basic Auth credentials via Authentication.InteractiveLogon() and Bitbucket AuthenticationPrompts.CredentialModalPrompt()
3. If that fails it, optionally, prompts of OAuth credentials via Authentication.InteractiveLogon() and Bitbucket AuthenticationPrompts.AuthenticationOAuthModalPrompt()

Any credentials, retreived from storage or entered by the user, are validated using Authentication.ValidateCredentials(). Only if the credentials pass validation are they returned to Git.

## Install

See Deploy

## Reject

See Erase

## Remove

Not effected by adding Bitbucket support.

## Store

Follows existing behaviour.

Then if a Bitbucket URL is identified an instance of the Bitbucket Authentication class is created and the Bitbucket Authentication.SetCredentials() method is called to stored the passed in credentials.

## Uninstall

See Remove

## Version

Not effected by adding Bitbucket suuport.

# GCM Processes

## Get Credentials

Authentication.GetCredentials() will attempt to retrieve credentials, a username and some form of token either an access token or a password, from the Windows Credentials Manager.
Because Bitbucket OAuth credentials include a stored refresh token, if the GCM is unable to retrieve the explicit credentials it will check to see if there is a stored refresh token. If one is found then it is used in an attempt to retrieve a new access token. If this is successful the new access and refresh tokens are stored and the access token is returned.

Refresh tokens are stored in the Windows Credentials Manager by using a special URL as the key, this is generated by adding the suffix "/refresh_token".

For example, the OAuth access token would be stored under the key "git:https://mminns@bitbucket.org/" and the refresh token would be stored under "git:https://mminns@bitbucket.org/refresh_token"

## Store Credentials

When the GCM is instructed to store credentials for a Bitbucket URL the GCM will try and store default credentials for the host, i.e. for "git:https://bitbucket.org/", with a username. This effectively copies user specific credentials to the default host credentials. The GCM will also then check for a user specific refresh token, if it exists it will copy it to a new default host refresh token entry.

If default host credentials already exist the GCM will NOT overwrite them.


## Validate Credentials

For Bitbucket URL the GCM will attempt to vaidate the credentials it has by making a call to the Bbucket REST API _https://api.bitbucket.org/2.0/user_. Because the the stored credentials do not contain any indication if they are for Basic Auth or OAuth the GCM will call first using Basic Auth headers and if that fails try with OAuth Bearer headers.
Finally if the OAuth Bearer headers fail, the GCM will check to see if there is a refresh token available, if there is it will attempt to get a new access token and re-set the stored credentials to match that.