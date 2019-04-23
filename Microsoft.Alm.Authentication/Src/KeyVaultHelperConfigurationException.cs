// ------------------------------------------------------------------------------
//  <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
// ------------------------------------------------------------------------------

using System;

namespace Microsoft.Azure.KeyVault.Helper
{
    /// <summary>
    /// This exception is thrown when KeyVaultHelper class can't read configuration from 
    /// config file or can't access cerificate store and retrieve certificate for KeyVault access.
    /// </summary>
    [Serializable]
    public class KeyVaultHelperConfigurationException : Exception
    {
        public KeyVaultHelperConfigurationException(string message)
            : base(message)
        {
        }

        public KeyVaultHelperConfigurationException(string message, Exception ex)
            : base(message + "  See inner exception for details.", ex)
        {
        }
    }

}
