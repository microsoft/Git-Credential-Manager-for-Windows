namespace Microsoft.Alm.Authentication
{
    public enum TokenType
    {
        Unknown = 0,
        /// <summary>
        /// Azure Directory Access Token
        /// </summary>
        [System.ComponentModel.Description("Azure Directory Access Token")]
        Access,
        /// <summary>
        /// Azure Directory Refresh Token
        /// </summary>
        [System.ComponentModel.Description("Azure Directory Refresh Token")]
        Refresh,
        /// <summary>
        /// Personal Access Token, can be compact or not.
        /// </summary>
        [System.ComponentModel.Description("Personal Access Token")]
        Personal,
        /// <summary>
        /// Federated Authentication (aka FedAuth) Token
        /// </summary>
        [System.ComponentModel.Description("Federated Authentication Token")]
        Federated,
        /// <summary>
        /// Used only for testing
        /// </summary>
        [System.ComponentModel.Description("Test-only Token")]
        Test,
    }
}
