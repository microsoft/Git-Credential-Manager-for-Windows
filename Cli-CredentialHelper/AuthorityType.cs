namespace Microsoft.TeamFoundation.CredentialHelper
{
    /// <summary>
    /// Type of authentication and identity authority expected.
    /// </summary>
    internal enum AuthorityType
    {
        /// <summary>
        /// Attempt to detect the authority automatically, fallback to <see cref="Basic"/> if 
        /// unable to detect an authority.
        /// </summary>
        Auto,
        /// <summary>
        /// Basic username and password scheme
        /// </summary>
        Basic,
        /// <summary>
        /// Username and password scheme using Microsoft's Live system
        /// </summary>
        MicrosoftAccount,
        /// <summary>
        /// Azure Directory Authentication based, including support for ADFS
        /// </summary>
        AzureDirectory,
        /// <summary>
        /// Operating system / network integrated authentication layer.
        /// </summary>
        Integrated,
    }
}
