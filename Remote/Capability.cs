using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Remote
{
    internal class Capability
    {
        public Capability(string name, CapabilityExecutionDelegate execute)
        {
            this.Execute = execute;
            this.Name = name;
        }

        public readonly string Name;
        public readonly CapabilityExecutionDelegate Execute;

        public bool IsMatch(string line)
        {
            return this.Name.StartsWith(line, StringComparison.OrdinalIgnoreCase);
        }
    }
}
