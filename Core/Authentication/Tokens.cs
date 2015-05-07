namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    internal sealed class Tokens
    {
        public Tokens(string accessToken, string refreshToken)
        {
            this.AccessToken = new Token(accessToken, TokenType.Access);
            this.RefeshToken = new Token(refreshToken, TokenType.Refresh);
        }
        public Tokens(IdentityModel.Clients.ActiveDirectory.AuthenticationResult authResult)
        {
            this.AccessToken = new Token(authResult, TokenType.Access);
            this.RefeshToken = new Token(authResult, TokenType.Refresh);
        }

        public readonly Token AccessToken;
        public readonly Token RefeshToken;
    }
}
