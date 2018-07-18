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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static System.Text.Encoding;

namespace Microsoft.Alm.Authentication
{
    public abstract class BaseSecureStore : Base
    {
        public const int AttributeMaxLengh = NativeMethods.Credential.AttributeMaxLengh;
        public const int PasswordMaxLength = NativeMethods.Credential.PasswordMaxLength;
        public const int StringMaxLength = NativeMethods.Credential.StringMaxLength;
        public const int UsernameMaxLength = NativeMethods.Credential.UsernameMaxLength;

        public static readonly char[] IllegalCharacters = new[] { ':', ';', '\\', '?', '@', '=', '&', '%', '$' };

        protected BaseSecureStore(RuntimeContext context)
            : base(context)
        { }

        protected bool Delete(string targetName)
        {
            try
            {
                if (!NativeMethods.CredDelete(targetName, NativeMethods.CredentialType.Generic, 0))
                {
                    int error = Marshal.GetLastWin32Error();
                    switch (error)
                    {
                        case NativeMethods.Win32Error.NotFound:
                        case NativeMethods.Win32Error.NoSuchLogonSession:
                            Trace.WriteLine($"credentials not found for '{targetName}'.");
                            break;

                        default:
                            throw new Win32Exception(error, "Failed to delete credentials for " + targetName);
                    }
                }
                else
                {
                    Trace.WriteLine($"credentials for '{targetName}' deleted from store.");
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
                return false;
            }

            return true;
        }

        protected IEnumerable<Secret> EnumerateCredentials(string @namespace)
        {
            foreach (var secure in Storage.EnumerateSecureData(@namespace))
            {
                if (Token.GetTypeFromFriendlyName(secure.Name, out TokenType type)
                    && Token.Deserialize(Context, secure.Data, type, out Token token))
                {
                    yield return token;
                }
                else
                {
                    var username = secure.Name;
                    var password = Unicode.GetString(secure.Data);

                    var credential = new Credential(username, password);

                    yield return credential;
                }
            }
        }

        protected abstract string GetTargetName(TargetUri targetUri);

        protected void PurgeCredentials(string @namespace)
        {
            Storage.TryPurgeSecureData(@namespace);
        }

        protected Credential ReadCredentials(string targetName)
        {
            if (targetName is null)
                throw new ArgumentNullException(nameof(targetName));

            try
            {
                if (Storage.TryReadSecureData(targetName, out string name, out byte[] data))
                {
                    string password = Unicode.GetString(data);

                    return new Credential(name, password);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);

                Trace.WriteException(exception);
            }

            return null;
        }

        protected Token ReadToken(string targetName)
        {
            if (targetName is null)
                throw new ArgumentNullException(nameof(targetName));

            try
            {
                if (Storage.TryReadSecureData(targetName, out string name, out byte[] data)
                    && Token.GetTypeFromFriendlyName(name, out TokenType type)
                    && Token.Deserialize(Context, data, type, out Token token))
                {
                    return token;
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);

                Trace.WriteException(exception);
            }

            return null;
        }

        protected bool WriteCredential(string targetName, Credential credentials)
        {
            if (targetName is null)
                throw new ArgumentNullException(nameof(targetName));
            if (credentials is null)
                throw new ArgumentNullException(nameof(credentials));

            try
            {
                string name = credentials.Username;
                byte[] data = Unicode.GetBytes(credentials.Password);

                return Storage.TryWriteSecureData(targetName, name, data);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);

                Trace.WriteException(exception);
            }

            return false;
        }

        protected bool WriteToken(string targetName, Token token)
        {
            if (targetName is null)
                throw new ArgumentNullException(nameof(targetName));
            if (token is null)
                throw new ArgumentNullException(nameof(token));

            if (Token.Serialize(Context, token, out byte[] data))
            {
                if (Token.GetFriendlyNameFromType(token.Type, out string name))
                {
                    try
                    {
                        return Storage.TryWriteSecureData(targetName, name, data);
                    }
                    catch (Exception exception)
                    {
                        Debug.WriteLine(exception);

                        Trace.WriteException(exception);
                    }
                }
            }

            return false;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void ValidateTargetUri(TargetUri targetUri)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (!targetUri.IsAbsoluteUri)
            {
                var innerException = new UriFormatException("URI is not an absolute.");
                throw new ArgumentException(innerException.Message, nameof(targetUri), innerException);
            }
        }
    }
}
