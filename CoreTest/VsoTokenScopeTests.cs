using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
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

            Assert.IsTrue(hashCodes.Add(VsoTokenScope.BuildAccess.GetHashCode()));
            Assert.IsTrue(hashCodes.Add(VsoTokenScope.BuildExecute.GetHashCode()));
            Assert.IsTrue(hashCodes.Add(VsoTokenScope.ChatManage.GetHashCode()));
            Assert.IsTrue(hashCodes.Add(VsoTokenScope.ChatWrite.GetHashCode()));
            Assert.IsTrue(hashCodes.Add(VsoTokenScope.CodeManage.GetHashCode()));
            Assert.IsTrue(hashCodes.Add(VsoTokenScope.CodeRead.GetHashCode()));
            Assert.IsTrue(hashCodes.Add(VsoTokenScope.CodeWrite.GetHashCode()));
            Assert.IsTrue(hashCodes.Add(VsoTokenScope.None.GetHashCode()));
            Assert.IsTrue(hashCodes.Add(VsoTokenScope.PackagingManage.GetHashCode()));
            Assert.IsTrue(hashCodes.Add(VsoTokenScope.PackagingRead.GetHashCode()));
            Assert.IsTrue(hashCodes.Add(VsoTokenScope.PackagingWrite.GetHashCode()));
            Assert.IsTrue(hashCodes.Add(VsoTokenScope.ProfileRead.GetHashCode()));
            Assert.IsTrue(hashCodes.Add(VsoTokenScope.ServiceHookRead.GetHashCode()));
            Assert.IsTrue(hashCodes.Add(VsoTokenScope.ServiceHookWrite.GetHashCode()));
            Assert.IsTrue(hashCodes.Add(VsoTokenScope.TestRead.GetHashCode()));
            Assert.IsTrue(hashCodes.Add(VsoTokenScope.TestWrite.GetHashCode()));
            Assert.IsTrue(hashCodes.Add(VsoTokenScope.WorkRead.GetHashCode()));
            Assert.IsTrue(hashCodes.Add(VsoTokenScope.WorkWrite.GetHashCode()));

            Assert.IsTrue(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.BuildAccess).GetHashCode()));
            Assert.IsTrue(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.BuildExecute).GetHashCode()));
            Assert.IsTrue(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.ChatManage).GetHashCode()));
            Assert.IsTrue(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.ChatWrite).GetHashCode()));
            Assert.IsTrue(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.CodeManage).GetHashCode()));
            Assert.IsFalse(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.CodeRead).GetHashCode()));
            Assert.IsTrue(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.CodeWrite).GetHashCode()));
            Assert.IsFalse(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.None).GetHashCode()));
            Assert.IsTrue(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.PackagingManage).GetHashCode()));
            Assert.IsTrue(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.PackagingRead).GetHashCode()));
            Assert.IsTrue(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.PackagingWrite).GetHashCode()));
            Assert.IsTrue(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.ProfileRead).GetHashCode()));
            Assert.IsTrue(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.ServiceHookRead).GetHashCode()));
            Assert.IsTrue(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.ServiceHookWrite).GetHashCode()));
            Assert.IsTrue(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.TestRead).GetHashCode()));
            Assert.IsTrue(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.TestWrite).GetHashCode()));
            Assert.IsTrue(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.WorkRead).GetHashCode()));
            Assert.IsTrue(hashCodes.Add((VsoTokenScope.CodeRead | VsoTokenScope.WorkWrite).GetHashCode()));
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
