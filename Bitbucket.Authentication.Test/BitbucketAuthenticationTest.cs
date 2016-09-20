using System;
using Microsoft.Alm.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bitbucket.Authentication.Test
{
    [TestClass]
    public class BitbucketAuthenticationTest
    {
        [TestMethod]
        public void VerifyBitbucketOrgIsIdentified()
        {
            var targetUri = new TargetUri("https://bitbucket.org");
            var bbAuth = BitbucketAuthentication.GetAuthentication(targetUri);

            Assert.IsNotNull(bbAuth);
        }

        [TestMethod]
        public void VerifyNonBitbucketOrgIsIgnored()
        {
            var targetUri = new TargetUri("https://example.com");
            var bbAuth = BitbucketAuthentication.GetAuthentication(targetUri);

            Assert.IsNull(bbAuth);
        }
    }
}
