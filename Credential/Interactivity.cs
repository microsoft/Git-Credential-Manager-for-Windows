namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    /// <summary>
    /// Level of interactivity allowed and enabled.
    /// </summary>
    internal enum Interactivity
    {
        /// <summary>
        /// Present an interactive logon prompt when necissary, otherwise used cached credentials
        /// </summary>
        Auto,
        /// <summary>
        /// Always present an interactive logon prompt regardless of if cached credentials exist
        /// </summary>
        Always,
        /// <summary>
        /// Never present an present an interactive logon prompt, fail without cached credentals
        /// </summary>
        Never
    }
}
