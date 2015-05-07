using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Remote
{
    partial class Program
    {
        static Capability[] Capabilities =
        {
            new Capability("connect", Program.ExecuteConnect),
            new Capability("push", Program.ExecutePush),
            new Capability("export", Program.ExecuteExport),
            new Capability("fetch", Program.ExecuteFetch),
            new Capability("import", Program.ExecuteImport),
            new Capability("option", Program.ExecuteOption),
            new Capability("refspec", Program.ExecuteRefspec),
            new Capability("bidi-import", Program.ExecuteBidiImport),
            new Capability("export-marks", Program.ExecuteExport),
            new Capability("import-marks", Program.ExecuteImportMarks),
        };

        static bool ExecuteConnect(OperationArguments arguments)
        {
            throw new NotImplementedException();
        }

        static bool ExecutePush(OperationArguments arguments)
        {
            throw new NotImplementedException();
        }

        static bool ExecuteExport(OperationArguments arguments)
        {
            throw new NotImplementedException();
        }

        static bool ExecuteFetch(OperationArguments arguments)
        {
            throw new NotImplementedException();
        }

        static bool ExecuteImport(OperationArguments arguments)
        {
            throw new NotImplementedException();
        }

        static bool ExecuteOption(OperationArguments arguments)
        {
            throw new NotImplementedException();
        }

        static bool ExecuteRefspec(OperationArguments arguments)
        {
            throw new NotImplementedException();
        }

        static bool ExecuteBidiImport(OperationArguments arguments)
        {
            throw new NotImplementedException();
        }

        static bool ExecuteExportMarks(OperationArguments arguments)
        {
            throw new NotImplementedException();
        }

        static bool ExecuteImportMarks(OperationArguments arguments)
        {
            throw new NotImplementedException();
        }
    }
}
