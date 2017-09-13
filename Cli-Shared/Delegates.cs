/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using Bitbucket = Atlassian.Bitbucket.Authentication;
using Github = GitHub.Authentication;

namespace Microsoft.Alm.Cli
{
    partial class Program
    {
        internal delegate Credential BasicCredentialPromptDelegate(Program program, TargetUri targetUri, string titleMessage);

        internal delegate bool BitbucketCredentialPromptDelegate(Program program, string titleMessage, TargetUri targetUri, out string username, out string password);

        internal delegate bool BitbucketOAuthPromptDelegate(Program program, string title, TargetUri targetUri, Bitbucket.AuthenticationResultType resultType, string username);

        internal delegate Task<BaseAuthentication> CreateAuthenticationDelegate(Program program, OperationArguments operationArguments);

        internal delegate void DeleteCredentialsDelegate(Program program, OperationArguments operationArguments);

        internal delegate void DieExceptionDelegate(Program program, Exception exception, string path, int line, string name);

        internal delegate void DieMessageDelegate(Program program, string message, string path, int line, string name);

        internal delegate void EnableTraceLoggingDelegate(Program program, OperationArguments operationArguments);

        internal delegate void EnableTraceLoggingFileDelegate(Program program, OperationArguments operationArguments, string logFilePath);

        internal delegate void ExitDelegate(Program program, int exitcode, string message, string path, int line, string name);

        internal delegate bool GitHubAuthCodePromptDelegate(Program program, TargetUri targetUri, Github.GitHubAuthenticationResultType resultType, string username, out string authenticationCode);

        internal delegate bool GitHubCredentialPromptDelegate(Program program, TargetUri targetUri, out string username, out string password);

        internal delegate void LoadOperationArgumentsDelegate(Program program, OperationArguments operationArguments);

        internal delegate void LogEventDelegate(Program program, string message, EventLogEntryType eventType);

        internal delegate bool ModalPromptDisplayDialogDelegate(Program program,
                                                                ref NativeMethods.CredentialUiInfo credUiInfo,
                                                                ref NativeMethods.CredentialPackFlags authPackage,
                                                                IntPtr packedAuthBufferPtr,
                                                                uint packedAuthBufferSize,
                                                                IntPtr inBufferPtr,
                                                                int inBufferSize,
                                                                bool saveCredentials,
                                                                NativeMethods.CredentialUiWindowsFlags flags,
                                                                out string username,
                                                                out string password);

        internal delegate Credential ModalPromptForCredentialsDelegate(Program program, TargetUri targetUri, string message);

        internal delegate Credential ModalPromptForPasswordDelegate(Program program, TargetUri targetUri, string message, string username);

        internal delegate void PrintArgsDelegate(Program program, string[] args);

        internal delegate Credential QueryCredentialsDelegate(Program program, OperationArguments operationArguments);

        internal delegate ConsoleKeyInfo ReadKeyDelegate(Program program, bool intercept);

        internal delegate bool StandardHandleIsTtyDelegate(Program program, NativeMethods.StandardHandleType handleType);

        internal delegate bool TryReadBooleanDelegate(Program program, OperationArguments operationArguments, string configKey, string environKey, out bool? value);

        internal delegate bool TryReadStringDelegate(Program program, OperationArguments operationArguments, string configKey, string environKey, out string value);

        internal delegate void WriteDelegate(Program program, string message);

        internal delegate void WriteLineDelegate(Program program, string message);
    }
}
