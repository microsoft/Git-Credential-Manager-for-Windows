using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    [TestClass]
    public class AuthenticationTests
    {
        [TestMethod]
        public void AuthenticationVsoAadTests()
        {
            VsoAadAuthentication vsoAad = new VsoAadAuthentication(new CredentialCache("pat"), new CredentialCache("user"), new TokenCache("adal"));
            
        }

        [TestMethod]
        public void AuthenticationVsoMsaTests()
        {
            Uri targetUri = new Uri("https://mseng.microsoft.com");
            Credential credentials = new Credential("jwyman@microsoft.com", "0thLight?");

            VsoMsaAuthentation vsoMsa = new VsoMsaAuthentation();
            Assert.IsTrue(vsoMsa.PromptLogon(targetUri), "Promp logon failed");
        }
    }
}
