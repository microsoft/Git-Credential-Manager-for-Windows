using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Alm.Authentication.Test
{
    [TestClass]
    public class VstsTokenScopeTests
    {
        [TestMethod]
        public void AddOperator()
        {
            var val = VstsTokenScope.BuildAccess + VstsTokenScope.TestRead;
            Assert.AreEqual(val.Value, VstsTokenScope.BuildAccess.Value + " " + VstsTokenScope.TestRead.Value);

            val += VstsTokenScope.ProfileRead;
            Assert.AreEqual(val.Value, VstsTokenScope.BuildAccess.Value + " " + VstsTokenScope.TestRead.Value + " " + VstsTokenScope.ProfileRead);
        }

        [TestMethod]
        public void AndOperator()
        {
            var val = (VstsTokenScope.BuildAccess & VstsTokenScope.BuildAccess);
            Assert.AreEqual(VstsTokenScope.BuildAccess, val);

            val = VstsTokenScope.ProfileRead + VstsTokenScope.PackagingWrite + VstsTokenScope.BuildAccess;
            Assert.IsTrue((val & VstsTokenScope.ProfileRead) == VstsTokenScope.ProfileRead);
            Assert.IsTrue((val & VstsTokenScope.PackagingWrite) == VstsTokenScope.PackagingWrite);
            Assert.IsTrue((val & VstsTokenScope.BuildAccess) == VstsTokenScope.BuildAccess);
            Assert.IsFalse((val & VstsTokenScope.PackagingManage) == VstsTokenScope.PackagingManage);
            Assert.IsTrue((val & VstsTokenScope.PackagingManage) == VstsTokenScope.None);
        }

        [TestMethod]
        public void Equality()
        {
            Assert.AreEqual(VstsTokenScope.CodeWrite, VstsTokenScope.CodeWrite);
            Assert.AreEqual(VstsTokenScope.None, VstsTokenScope.None);

            Assert.AreNotEqual(VstsTokenScope.BuildAccess, VstsTokenScope.CodeRead);
            Assert.AreNotEqual(VstsTokenScope.BuildAccess, VstsTokenScope.None);

            Assert.AreEqual(VstsTokenScope.PackagingManage | VstsTokenScope.PackagingRead | VstsTokenScope.PackagingWrite, VstsTokenScope.PackagingManage | VstsTokenScope.PackagingRead | VstsTokenScope.PackagingWrite);
            Assert.AreEqual(VstsTokenScope.PackagingWrite | VstsTokenScope.PackagingManage | VstsTokenScope.PackagingRead, VstsTokenScope.PackagingManage | VstsTokenScope.PackagingRead | VstsTokenScope.PackagingWrite);

            Assert.AreNotEqual(VstsTokenScope.PackagingManage | VstsTokenScope.ServiceHookRead | VstsTokenScope.PackagingWrite, VstsTokenScope.PackagingManage | VstsTokenScope.PackagingRead | VstsTokenScope.PackagingWrite);
            Assert.AreNotEqual(VstsTokenScope.PackagingManage | VstsTokenScope.PackagingRead | VstsTokenScope.PackagingWrite, VstsTokenScope.PackagingManage | VstsTokenScope.PackagingRead);
        }

        [TestMethod]
        public void HashCode()
        {
            HashSet<int> hashCodes = new HashSet<int>();

            foreach (var item in VstsTokenScope.EnumerateValues())
            {
                Assert.IsTrue(hashCodes.Add(item.GetHashCode()));
            }

            int loop1 = 0;
            foreach (var item1 in VstsTokenScope.EnumerateValues())
            {
                int loop2 = 0;

                foreach (var item2 in VstsTokenScope.EnumerateValues())
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
            var val1 = (VstsTokenScope.BuildAccess | VstsTokenScope.BuildAccess);
            Assert.AreEqual(VstsTokenScope.BuildAccess, val1);

            val1 = VstsTokenScope.ProfileRead + VstsTokenScope.PackagingWrite + VstsTokenScope.BuildAccess;
            var val2 = val1 | VstsTokenScope.ProfileRead;
            Assert.AreEqual(val1, val2);

            val2 = VstsTokenScope.ProfileRead | VstsTokenScope.PackagingWrite | VstsTokenScope.BuildAccess;
            Assert.AreEqual(val1, val2);
            Assert.IsTrue((val2 & VstsTokenScope.ProfileRead) == VstsTokenScope.ProfileRead);
            Assert.IsTrue((val2 & VstsTokenScope.PackagingWrite) == VstsTokenScope.PackagingWrite);
            Assert.IsTrue((val2 & VstsTokenScope.BuildAccess) == VstsTokenScope.BuildAccess);
            Assert.IsFalse((val2 & VstsTokenScope.PackagingManage) == VstsTokenScope.PackagingManage);
        }

        [TestMethod]
        public void MinusOpertor()
        {
            var val1 = VstsTokenScope.BuildAccess | VstsTokenScope.BuildExecute | VstsTokenScope.ChatWrite;
            var val2 = val1 - VstsTokenScope.ChatWrite;
            Assert.AreEqual(val2, VstsTokenScope.BuildAccess | VstsTokenScope.BuildExecute);

            var val3 = val1 - val2;
            Assert.AreEqual(val3, VstsTokenScope.ChatWrite);

            var val4 = val3 - VstsTokenScope.ChatManage;
            Assert.AreEqual(val3, val4);

            var val5 = (VstsTokenScope.BuildAccess + VstsTokenScope.BuildExecute) - (VstsTokenScope.BuildExecute | VstsTokenScope.CodeManage | VstsTokenScope.CodeWrite);
            Assert.AreEqual(val5, VstsTokenScope.BuildAccess);
        }

        [TestMethod]
        public void XorOperator()
        {
            var val1 = VstsTokenScope.ChatWrite + VstsTokenScope.CodeRead;
            var val2 = VstsTokenScope.CodeRead + VstsTokenScope.PackagingRead;
            var val3 = val1 ^ val2;
            Assert.AreEqual(val3, VstsTokenScope.ChatWrite | VstsTokenScope.PackagingRead);
        }
    }
}
