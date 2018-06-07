/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

using System.Collections.Generic;
using Xunit;

namespace VisualStudioTeamServices.Authentication.Test
{
    public class VstsTokenScopeTests
    {
        [Fact]
        public void AddOperator()
        {
            var val = VstsTokenScope.BuildAccess + VstsTokenScope.TestRead;
            Assert.Equal(val.Value, VstsTokenScope.BuildAccess.Value + " " + VstsTokenScope.TestRead.Value);

            val += VstsTokenScope.ProfileRead;
            Assert.Equal(val.Value, VstsTokenScope.BuildAccess.Value + " " + VstsTokenScope.TestRead.Value + " " + VstsTokenScope.ProfileRead);
        }

        [Fact]
        public void AndOperator()
        {
            var val = (VstsTokenScope.BuildAccess & VstsTokenScope.BuildAccess);
            Assert.Equal(VstsTokenScope.BuildAccess, val);

            val = VstsTokenScope.ProfileRead + VstsTokenScope.PackagingWrite + VstsTokenScope.BuildAccess;
            Assert.True((val & VstsTokenScope.ProfileRead) == VstsTokenScope.ProfileRead);
            Assert.True((val & VstsTokenScope.PackagingWrite) == VstsTokenScope.PackagingWrite);
            Assert.True((val & VstsTokenScope.BuildAccess) == VstsTokenScope.BuildAccess);
            Assert.False((val & VstsTokenScope.PackagingManage) == VstsTokenScope.PackagingManage);
            Assert.True((val & VstsTokenScope.PackagingManage) == VstsTokenScope.None);
        }

        [Fact]
        public void Equality()
        {
            Assert.Equal(VstsTokenScope.CodeWrite, VstsTokenScope.CodeWrite);
            Assert.Equal(VstsTokenScope.None, VstsTokenScope.None);

            Assert.NotEqual(VstsTokenScope.BuildAccess, VstsTokenScope.CodeRead);
            Assert.NotEqual(VstsTokenScope.BuildAccess, VstsTokenScope.None);

            Assert.Equal(VstsTokenScope.PackagingManage | VstsTokenScope.PackagingRead | VstsTokenScope.PackagingWrite, VstsTokenScope.PackagingManage | VstsTokenScope.PackagingRead | VstsTokenScope.PackagingWrite);
            Assert.Equal(VstsTokenScope.PackagingWrite | VstsTokenScope.PackagingManage | VstsTokenScope.PackagingRead, VstsTokenScope.PackagingManage | VstsTokenScope.PackagingRead | VstsTokenScope.PackagingWrite);

            Assert.NotEqual(VstsTokenScope.PackagingManage | VstsTokenScope.ServiceHookRead | VstsTokenScope.PackagingWrite, VstsTokenScope.PackagingManage | VstsTokenScope.PackagingRead | VstsTokenScope.PackagingWrite);
            Assert.NotEqual(VstsTokenScope.PackagingManage | VstsTokenScope.PackagingRead | VstsTokenScope.PackagingWrite, VstsTokenScope.PackagingManage | VstsTokenScope.PackagingRead);
        }

        [Fact]
        public void HashCode()
        {
            HashSet<int> hashCodes = new HashSet<int>();

            foreach (var item in VstsTokenScope.EnumerateValues())
            {
                Assert.True(hashCodes.Add(item.GetHashCode()));
            }

            int loop1 = 0;
            foreach (var item1 in VstsTokenScope.EnumerateValues())
            {
                int loop2 = 0;

                foreach (var item2 in VstsTokenScope.EnumerateValues())
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
            var val1 = (VstsTokenScope.BuildAccess | VstsTokenScope.BuildAccess);
            Assert.Equal(VstsTokenScope.BuildAccess, val1);

            val1 = VstsTokenScope.ProfileRead + VstsTokenScope.PackagingWrite + VstsTokenScope.BuildAccess;
            var val2 = val1 | VstsTokenScope.ProfileRead;
            Assert.Equal(val1, val2);

            val2 = VstsTokenScope.ProfileRead | VstsTokenScope.PackagingWrite | VstsTokenScope.BuildAccess;
            Assert.Equal(val1, val2);
            Assert.True((val2 & VstsTokenScope.ProfileRead) == VstsTokenScope.ProfileRead);
            Assert.True((val2 & VstsTokenScope.PackagingWrite) == VstsTokenScope.PackagingWrite);
            Assert.True((val2 & VstsTokenScope.BuildAccess) == VstsTokenScope.BuildAccess);
            Assert.False((val2 & VstsTokenScope.PackagingManage) == VstsTokenScope.PackagingManage);
        }

        [Fact]
        public void MinusOperator()
        {
            var val1 = VstsTokenScope.BuildAccess | VstsTokenScope.BuildExecute | VstsTokenScope.ChatWrite;
            var val2 = val1 - VstsTokenScope.ChatWrite;
            Assert.Equal(val2, VstsTokenScope.BuildAccess | VstsTokenScope.BuildExecute);

            var val3 = val1 - val2;
            Assert.Equal(val3, VstsTokenScope.ChatWrite);

            var val4 = val3 - VstsTokenScope.ChatManage;
            Assert.Equal(val3, val4);

            var val5 = (VstsTokenScope.BuildAccess + VstsTokenScope.BuildExecute) - (VstsTokenScope.BuildExecute | VstsTokenScope.CodeManage | VstsTokenScope.CodeWrite);
            Assert.Equal(val5, VstsTokenScope.BuildAccess);
        }

        [Fact]
        public void XorOperator()
        {
            var val1 = VstsTokenScope.ChatWrite + VstsTokenScope.CodeRead;
            var val2 = VstsTokenScope.CodeRead + VstsTokenScope.PackagingRead;
            var val3 = val1 ^ val2;
            Assert.Equal(val3, VstsTokenScope.ChatWrite | VstsTokenScope.PackagingRead);
        }
    }
}
