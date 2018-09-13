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

namespace AzureDevOps.Authentication.Test
{
    public class TokenScopeTests
    {
        [Fact]
        public void AddOperator()
        {
            var val = TokenScope.BuildAccess + TokenScope.TestRead;
            Assert.Equal(val.Value, TokenScope.BuildAccess.Value + " " + TokenScope.TestRead.Value);

            val += TokenScope.ProfileRead;
            Assert.Equal(val.Value, TokenScope.BuildAccess.Value + " " + TokenScope.TestRead.Value + " " + TokenScope.ProfileRead);
        }

        [Fact]
        public void AndOperator()
        {
            var val = (TokenScope.BuildAccess & TokenScope.BuildAccess);
            Assert.Equal(TokenScope.BuildAccess, val);

            val = TokenScope.ProfileRead + TokenScope.PackagingWrite + TokenScope.BuildAccess;
            Assert.True((val & TokenScope.ProfileRead) == TokenScope.ProfileRead);
            Assert.True((val & TokenScope.PackagingWrite) == TokenScope.PackagingWrite);
            Assert.True((val & TokenScope.BuildAccess) == TokenScope.BuildAccess);
            Assert.False((val & TokenScope.PackagingManage) == TokenScope.PackagingManage);
            Assert.True((val & TokenScope.PackagingManage) == TokenScope.None);
        }

        [Fact]
        public void Equality()
        {
            Assert.Equal(TokenScope.CodeWrite, TokenScope.CodeWrite);
            Assert.Equal(TokenScope.None, TokenScope.None);

            Assert.NotEqual(TokenScope.BuildAccess, TokenScope.CodeRead);
            Assert.NotEqual(TokenScope.BuildAccess, TokenScope.None);

            Assert.Equal(TokenScope.PackagingManage | TokenScope.PackagingRead | TokenScope.PackagingWrite, TokenScope.PackagingManage | TokenScope.PackagingRead | TokenScope.PackagingWrite);
            Assert.Equal(TokenScope.PackagingWrite | TokenScope.PackagingManage | TokenScope.PackagingRead, TokenScope.PackagingManage | TokenScope.PackagingRead | TokenScope.PackagingWrite);

            Assert.NotEqual(TokenScope.PackagingManage | TokenScope.ServiceHookRead | TokenScope.PackagingWrite, TokenScope.PackagingManage | TokenScope.PackagingRead | TokenScope.PackagingWrite);
            Assert.NotEqual(TokenScope.PackagingManage | TokenScope.PackagingRead | TokenScope.PackagingWrite, TokenScope.PackagingManage | TokenScope.PackagingRead);
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
            var val1 = (TokenScope.BuildAccess | TokenScope.BuildAccess);
            Assert.Equal(TokenScope.BuildAccess, val1);

            val1 = TokenScope.ProfileRead + TokenScope.PackagingWrite + TokenScope.BuildAccess;
            var val2 = val1 | TokenScope.ProfileRead;
            Assert.Equal(val1, val2);

            val2 = TokenScope.ProfileRead | TokenScope.PackagingWrite | TokenScope.BuildAccess;
            Assert.Equal(val1, val2);
            Assert.True((val2 & TokenScope.ProfileRead) == TokenScope.ProfileRead);
            Assert.True((val2 & TokenScope.PackagingWrite) == TokenScope.PackagingWrite);
            Assert.True((val2 & TokenScope.BuildAccess) == TokenScope.BuildAccess);
            Assert.False((val2 & TokenScope.PackagingManage) == TokenScope.PackagingManage);
        }

        [Fact]
        public void MinusOperator()
        {
            var val1 = TokenScope.BuildAccess | TokenScope.BuildExecute | TokenScope.ChatWrite;
            var val2 = val1 - TokenScope.ChatWrite;
            Assert.Equal(val2, TokenScope.BuildAccess | TokenScope.BuildExecute);

            var val3 = val1 - val2;
            Assert.Equal(val3, TokenScope.ChatWrite);

            var val4 = val3 - TokenScope.ChatManage;
            Assert.Equal(val3, val4);

            var val5 = (TokenScope.BuildAccess + TokenScope.BuildExecute) - (TokenScope.BuildExecute | TokenScope.CodeManage | TokenScope.CodeWrite);
            Assert.Equal(val5, TokenScope.BuildAccess);
        }

        [Fact]
        public void XorOperator()
        {
            var val1 = TokenScope.ChatWrite + TokenScope.CodeRead;
            var val2 = TokenScope.CodeRead + TokenScope.PackagingRead;
            var val3 = val1 ^ val2;
            Assert.Equal(val3, TokenScope.ChatWrite | TokenScope.PackagingRead);
        }
    }
}
