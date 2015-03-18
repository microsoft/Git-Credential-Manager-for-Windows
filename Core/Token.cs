namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public sealed class Token
    {
        public Token(string value)
        {
            this.Value = value;
        }

        public readonly string Value;
    }
}
