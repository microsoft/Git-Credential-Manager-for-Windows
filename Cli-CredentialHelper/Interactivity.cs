namespace Microsoft.Alm.CredentialHelper
{
    /// <summary>
    /// Level of interactivity allowed and enabled.
    /// </summary>
    internal enum Interactivity
    {
        /// <summary>
        /// Present an interactive logon prompt when necessary, otherwise use cached credentials
        /// </summary>
        Auto,
        /// <summary>
        /// Always present an interactive logon prompt regardless if cached credentials exist
        /// </summary>
        Always,
        /// <summary>
        /// Never present an present an interactive logon prompt, fail without cached credentials
        /// </summary>
        Never
    }
}
