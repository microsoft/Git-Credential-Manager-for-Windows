using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TeamFoundation.Authentication.Test
{
    [TestClass]
    public class VsoTokenScopeTests
    {
        [TestMethod]
        public void AddOperator()
        {
            var val = VsoTokenScope.BuildAccess + VsoTokenScope.TestRead;
            Assert.AreEqual(val.Value, VsoTokenScope.BuildAccess.Value + " " + VsoTokenScope.TestRead.Value);

            val += VsoTokenScope.ProfileRead;
            Assert.AreEqual(val.Value, VsoTokenScope.BuildAccess.Value + " " + VsoTokenScope.TestRead.Value + " " + VsoTokenScope.ProfileRead);
        }

        [TestMethod]
        public void AndOperator()
        {
            var val = (VsoTokenScope.BuildAccess & VsoTokenScope.BuildAccess);
            Assert.AreEqual(VsoTokenScope.BuildAccess, val);

            val = VsoTokenScope.ProfileRead + VsoTokenScope.PackagingWrite + VsoTokenScope.BuildAccess;
            Assert.IsTrue((val & VsoTokenScope.ProfileRead) == VsoTokenScope.ProfileRead);
            Assert.IsTrue((val & VsoTokenScope.PackagingWrite) == VsoTokenScope.PackagingWrite);
            Assert.IsTrue((val & VsoTokenScope.BuildAccess) == VsoTokenScope.BuildAccess);
            Assert.IsFalse((val & VsoTokenScope.PackagingManage) == VsoTokenScope.PackagingManage);
            Assert.IsTrue((val & VsoTokenScope.PackagingManage) == VsoTokenScope.None);
        }

        [TestMethod]
        public void Equality()
        {
            Assert.AreEqual(VsoTokenScope.CodeWrite, VsoTokenScope.CodeWrite);
            Assert.AreEqual(VsoTokenScope.None, VsoTokenScope.None);

            Assert.AreNotEqual(VsoTokenScope.BuildAccess, VsoTokenScope.CodeRead);
            Assert.AreNotEqual(VsoTokenScope.BuildAccess, VsoTokenScope.None);

            Assert.AreEqual(VsoTokenScope.PackagingManage | VsoTokenScope.PackagingRead | VsoTokenScope.PackagingWrite, VsoTokenScope.PackagingManage | VsoTokenScope.PackagingRead | VsoTokenScope.PackagingWrite);
            Assert.AreEqual(VsoTokenScope.PackagingWrite | VsoTokenScope.PackagingManage | VsoTokenScope.PackagingRead, VsoTokenScope.PackagingManage | VsoTokenScope.PackagingRead | VsoTokenScope.PackagingWrite);

            Assert.AreNotEqual(VsoTokenScope.PackagingManage | VsoTokenScope.ServiceHookRead | VsoTokenScope.PackagingWrite, VsoTokenScope.PackagingManage | VsoTokenScope.PackagingRead | VsoTokenScope.PackagingWrite);
            Assert.AreNotEqual(VsoTokenScope.PackagingManage | VsoTokenScope.PackagingRead | VsoTokenScope.PackagingWrite, VsoTokenScope.PackagingManage | VsoTokenScope.PackagingRead);
        }

        [TestMethod]
        public void HashCode()
        {
            HashSet<int> hashCodes = new HashSet<int>();

            foreach (var item in VsoTokenScope.EnumerateValues())
            {
                Assert.IsTrue(hashCodes.Add(item.GetHashCode()));
            }

            int loop1 = 0;
            foreach (var item1 in VsoTokenScope.EnumerateValues())
            {
                int loop2 = 0;

                foreach (var item2 in VsoTokenScope.EnumerateValues())
                {
                    if (loop1 < loop2)
                    {
                        Assert.IsTrue(hashCodes.Add((item1 | item2).GetHashCode()));
                    }
                    else
                    {
                        Assert.IsFalse(hashCodes.Add((item1 | item2).GetHashCode()));
                    }

                    loop2++;
                }

                loop1++;
            }
        }

        [TestMethod]
        public void OrOperator()
        {
            var val1 = (VsoTokenScope.BuildAccess | VsoTokenScope.BuildAccess);
            Assert.AreEqual(VsoTokenScope.BuildAccess, val1);

            val1 = VsoTokenScope.ProfileRead + VsoTokenScope.PackagingWrite + VsoTokenScope.BuildAccess;
            var val2 = val1 | VsoTokenScope.ProfileRead;
            Assert.AreEqual(val1, val2);

            val2 = VsoTokenScope.ProfileRead | VsoTokenScope.PackagingWrite | VsoTokenScope.BuildAccess;
            Assert.AreEqual(val1, val2);
            Assert.IsTrue((val2 & VsoTokenScope.ProfileRead) == VsoTokenScope.ProfileRead);
            Assert.IsTrue((val2 & VsoTokenScope.PackagingWrite) == VsoTokenScope.PackagingWrite);
            Assert.IsTrue((val2 & VsoTokenScope.BuildAccess) == VsoTokenScope.BuildAccess);
            Assert.IsFalse((val2 & VsoTokenScope.PackagingManage) == VsoTokenScope.PackagingManage);
        }

        [TestMethod]
        public void MinusOpertor()
        {
            var val1 = VsoTokenScope.BuildAccess | VsoTokenScope.BuildExecute | VsoTokenScope.ChatWrite;
            var val2 = val1 - VsoTokenScope.ChatWrite;
            Assert.AreEqual(val2, VsoTokenScope.BuildAccess | VsoTokenScope.BuildExecute);

            var val3 = val1 - val2;
            Assert.AreEqual(val3, VsoTokenScope.ChatWrite);

            var val4 = val3 - VsoTokenScope.ChatManage;
            Assert.AreEqual(val3, val4);

            var val5 = (VsoTokenScope.BuildAccess + VsoTokenScope.BuildExecute) - (VsoTokenScope.BuildExecute | VsoTokenScope.CodeManage | VsoTokenScope.CodeWrite);
            Assert.AreEqual(val5, VsoTokenScope.BuildAccess);
        }

        [TestMethod]
        public void XorOperator()
        {
            var val1 = VsoTokenScope.ChatWrite + VsoTokenScope.CodeRead;
            var val2 = VsoTokenScope.CodeRead + VsoTokenScope.PackagingRead;
            var val3 = val1 ^ val2;
            Assert.AreEqual(val3, VsoTokenScope.ChatWrite | VsoTokenScope.PackagingRead);
        }
    }
}
