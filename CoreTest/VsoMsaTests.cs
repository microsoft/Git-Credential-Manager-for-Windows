using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    [TestClass]
    public class VsoMsaTests
    {
        [TestMethod]
        public void VsoMsaInteractiveLogonTest()
        {
            VsoMsaAuthentation msa = new VsoMsaAuthentation();
            msa.InteractiveLogon(new Uri("https://dev-x.visualstudio.com"));
        }

        [TestMethod]
        public void VsoMsaRefreshCredentialsTest()
        {

        }

        [TestMethod]
        public void VsoMsaSetCredentialsTest()
        {

        }
    }
}
