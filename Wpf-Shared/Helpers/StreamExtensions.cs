using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlassian.Shared.Authentication.Helpers
{
    public static class StreamExtensions
    {
        public static void WriteStringUtf8(this Stream target, string value)
        {
            var encoded = Encoding.UTF8.GetBytes(value);
            target.Write(encoded, 0, encoded.Length);
        }
    }
}
