using System;

namespace Microsoft.TeamFoundation.Authentication
{
    public sealed  class GithubTokenScope : TokenScope
    {
        public static readonly GithubTokenScope None = new GithubTokenScope(String.Empty);

        private GithubTokenScope(string value)
            : base(value)
        { }
    }
}
