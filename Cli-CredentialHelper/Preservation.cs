namespace Microsoft.Alm.CredentialHelper
{
    /// <summary>
    /// Specialized handling of DELETE and STORE requests from Git
    /// </summary>
    internal enum Preservation
    {
        /// <summary>
        /// Normal handling of DELETE and STORE requests from Git
        /// </summary>
        Normal,
        /// <summary>
        /// Ignore DELETE requests from Git
        /// </summary>
        NeverDelete,
        /// <summary>
        /// Ignore STORE requests from Git
        /// </summary>
        NeverStore,
    }
}
