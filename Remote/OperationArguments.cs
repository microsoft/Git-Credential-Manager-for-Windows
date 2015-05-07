using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Remote
{
    internal class OperationArguments
    {
        public OperationArguments(Stream stdin, Stream stdout)
        {
            this.StdIn = stdin;
            this.StdOut = stdout;
        }

        public readonly Stream StdIn;
        public readonly Stream StdOut;
    }
}
