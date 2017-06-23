using System.Collections.Generic;
using Xunit;

namespace GitHub.Authentication.Test
{
    public class GitHubTokenScopeTests
    {
        [Fact]
        public void AddOperator()
        {
            var val = TokenScope.Gist + TokenScope.Notifications;
            Assert.Equal(val.Value, TokenScope.Gist.Value + " " + TokenScope.Notifications.Value);

            val += TokenScope.OrgAdmin;
            Assert.Equal(val.Value, TokenScope.Gist.Value + " " + TokenScope.Notifications.Value + " " + TokenScope.OrgAdmin);
        }

        [Fact]
        public void AndOperator()
        {
            var val = (TokenScope.Gist & TokenScope.Gist);
            Assert.Equal(TokenScope.Gist, val);

            val = TokenScope.OrgAdmin + TokenScope.OrgHookAdmin + TokenScope.Gist;
            Assert.True((val & TokenScope.OrgAdmin) == TokenScope.OrgAdmin);
            Assert.True((val & TokenScope.OrgHookAdmin) == TokenScope.OrgHookAdmin);
            Assert.True((val & TokenScope.Gist) == TokenScope.Gist);
            Assert.False((val & TokenScope.OrgRead) == TokenScope.OrgRead);
            Assert.True((val & TokenScope.OrgRead) == TokenScope.None);
        }

        [Fact]
        public void Equality()
        {
            Assert.Equal(TokenScope.OrgWrite, TokenScope.OrgWrite);
            Assert.Equal(TokenScope.None, TokenScope.None);

            Assert.NotEqual(TokenScope.Gist, TokenScope.PublicKeyAdmin);
            Assert.NotEqual(TokenScope.Gist, TokenScope.None);

            Assert.Equal(TokenScope.OrgRead | TokenScope.PublicKeyRead | TokenScope.OrgHookAdmin, TokenScope.OrgRead | TokenScope.PublicKeyRead | TokenScope.OrgHookAdmin);
            Assert.Equal(TokenScope.OrgHookAdmin | TokenScope.OrgRead | TokenScope.PublicKeyRead, TokenScope.OrgRead | TokenScope.PublicKeyRead | TokenScope.OrgHookAdmin);

            Assert.NotEqual(TokenScope.OrgRead | TokenScope.PublicKeyWrite | TokenScope.OrgHookAdmin, TokenScope.OrgRead | TokenScope.PublicKeyRead | TokenScope.OrgHookAdmin);
            Assert.NotEqual(TokenScope.OrgRead | TokenScope.PublicKeyRead | TokenScope.OrgHookAdmin, TokenScope.OrgRead | TokenScope.PublicKeyRead);
        }

        [Fact]
        public void HashCode()
        {
            HashSet<int> hashCodes = new HashSet<int>();

            foreach (var item in TokenScope.EnumerateValues())
            {
                Assert.True(hashCodes.Add(item.GetHashCode()));
            }

            int loop1 = 0;
            foreach (var item1 in TokenScope.EnumerateValues())
            {
                int loop2 = 0;

                foreach (var item2 in TokenScope.EnumerateValues())
                {
                    if (loop1 < loop2)
                    {
                        Assert.True(hashCodes.Add((item1 | item2).GetHashCode()));
                    }
                    else
                    {
                        Assert.False(hashCodes.Add((item1 | item2).GetHashCode()));
                    }

                    loop2++;
                }

                loop1++;
            }
        }

        [Fact]
        public void OrOperator()
        {
            var val1 = (TokenScope.Gist | TokenScope.Gist);
            Assert.Equal(TokenScope.Gist, val1);

            val1 = TokenScope.OrgAdmin + TokenScope.OrgHookAdmin + TokenScope.Gist;
            var val2 = val1 | TokenScope.OrgAdmin;
            Assert.Equal(val1, val2);

            val2 = TokenScope.OrgAdmin | TokenScope.OrgHookAdmin | TokenScope.Gist;
            Assert.Equal(val1, val2);
            Assert.True((val2 & TokenScope.OrgAdmin) == TokenScope.OrgAdmin);
            Assert.True((val2 & TokenScope.OrgHookAdmin) == TokenScope.OrgHookAdmin);
            Assert.True((val2 & TokenScope.Gist) == TokenScope.Gist);
            Assert.False((val2 & TokenScope.OrgRead) == TokenScope.OrgRead);
        }

        [Fact]
        public void MinusOperator()
        {
            var val1 = TokenScope.Gist | TokenScope.Repo | TokenScope.RepoDelete;
            var val2 = val1 - TokenScope.RepoDelete;
            Assert.Equal(val2, TokenScope.Gist | TokenScope.Repo);

            var val3 = val1 - val2;
            Assert.Equal(val3, TokenScope.RepoDelete);

            var val4 = val3 - TokenScope.RepoDeployment;
            Assert.Equal(val3, val4);

            var val5 = (TokenScope.Gist + TokenScope.Repo) - (TokenScope.Repo | TokenScope.RepoHookAdmin | TokenScope.OrgWrite);
            Assert.Equal(val5, TokenScope.Gist);
        }

        [Fact]
        public void XorOperator()
        {
            var val1 = TokenScope.RepoDelete + TokenScope.PublicKeyAdmin;
            var val2 = TokenScope.PublicKeyAdmin + TokenScope.PublicKeyRead;
            var val3 = val1 ^ val2;
            Assert.Equal(val3, TokenScope.RepoDelete | TokenScope.PublicKeyRead);
        }
    }
}
