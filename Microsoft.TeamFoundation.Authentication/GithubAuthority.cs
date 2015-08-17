using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Authentication
{
    internal class GithubAuthority
    {
        /// <summary>
        /// The maximum wait time for a network request before timing out
        /// </summary>
        public const int RequestTimeout = 15 * 1000; // 15 second limit

        public async Task AcquireToken()
        {

        }

        public async Task<Token> GeneratePersonalAccessToken(Uri targetUri, Token accessToken, GithubTokenScope tokenScope)
        {
            throw new NotImplementedException();
        }
    }
}
