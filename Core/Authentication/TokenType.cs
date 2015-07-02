namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public enum TokenType
    {
        [System.ComponentModel.Description("Azure Directory Access Token")]
        Access,
        [System.ComponentModel.Description("Azure Directory Refresh Token")]
        Refresh,
        [System.ComponentModel.Description("Personal Access Token")]
        VsoPat,
        [System.ComponentModel.Description("Test-only Token")]
        Test
    }
}
