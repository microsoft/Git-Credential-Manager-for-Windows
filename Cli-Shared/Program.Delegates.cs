using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using Microsoft.Alm.Git;
using Microsoft.Win32.SafeHandles;
using Bitbucket = Atlassian.Bitbucket.Authentication;
using Github = GitHub.Authentication;

namespace Microsoft.Alm.Cli
{
    partial class Program
    {
        internal delegate Credential BasicCredentialPromptDelegate(TargetUri targetUri, string titleMessage);
        internal delegate bool BitbucketCredentialPromptDelegate(string titleMessage, TargetUri targetUri, out string username, out string password);
        internal delegate bool BitbucketOAuthPromptDelegate(string title, TargetUri targetUri, Bitbucket.AuthenticationResultType resultType, string username);
        internal delegate Task<BaseAuthentication> CreateAuthenticationDelegate(OperationArguments operationArguments);
        internal delegate void DeleteCredentialsDelegate(OperationArguments operationArguments);
        internal delegate void DieExceptionDelegate(Exception exception, string path, int line, string name);
        internal delegate void DieMessageDelegate(string message, string path, int line, string name);
        internal delegate void EnableTraceLoggingDelegate(OperationArguments operationArguments);
        internal delegate void EnableTraceLoggingFileDelegate(OperationArguments operationArguments, string logFilePath);
        internal delegate void ExitDelegate(int exitcode, string message, string path, int line, string name);
        internal delegate bool GitHubAuthCodePromptDelegate(TargetUri targetUri, Github.GitHubAuthenticationResultType resultType, string username, out string authenticationCode);
        internal delegate bool GitHubCredentialPromptDelegate(TargetUri targetUri, out string username, out string password);
        internal delegate void LoadOperationArgumentsDelegate(OperationArguments operationArguments);
        internal delegate void LogEventDelegate(string message, EventLogEntryType eventType);
        internal delegate bool ModalPromptDisplayDialogDelegate(ref NativeMethods.CredentialUiInfo credUiInfo,
                                                                ref NativeMethods.CredentialPackFlags authPackage,
                                                                IntPtr packedAuthBufferPtr,
                                                                uint packedAuthBufferSize,
                                                                IntPtr inBufferPtr,
                                                                int inBufferSize,
                                                                bool saveCredentials,
                                                                NativeMethods.CredentialUiWindowsFlags flags,
                                                                out string username,
                                                                out string password);
        internal delegate Credential ModalPromptForCredentialsDelegate(TargetUri targetUri, string message);
        internal delegate Credential ModalPromptForPasswordDelegate(TargetUri targetUri, string message, string username);
        internal delegate void PrintArgsDelegate(string[] args);
        internal delegate Credential QueryCredentialsDelegate(OperationArguments operationArguments);
        internal delegate ConsoleKeyInfo ReadKeyDelegate(bool intercept);
        internal delegate bool StandardHandleIsTtyDelegate(NativeMethods.StandardHandleType handleType);
        internal delegate bool TryReadBooleanDelegate(OperationArguments operationArguments, string configKey, string environKey, out bool? value);
        internal delegate bool TryReadStringDelegate(OperationArguments operationArguments, string configKey, string environKey, out string value);
        internal delegate void WriteDelegate(string message);
        internal delegate void WriteLineDelegate(string message);
    }
}
