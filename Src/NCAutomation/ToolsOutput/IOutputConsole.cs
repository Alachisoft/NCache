using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alachisoft.NCache.Automation.ToolsOutput
{
    public interface IOutputConsole
    {
        void WriteLine(object message);
        void WriteLine(string message);
        void WriteLine(string format, string message);
        void WriteLine(string format, object arg0, object arg1);
        void WriteLine(string format, object arg0, object arg1, object arg2);
        void WriteLine(string format, object arg0, object arg1, object arg2, object arg3);
        void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4);
        void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9);
        void WriteLine(string message, object args0);
        void WriteErrorLine(object message);
        void WriteErrorLine(string message);
        void WriteErrorLine(string format, string message);
        void WriteErrorLine(string format, object arg0, object arg1);
        void WriteErrorLine(string format, object arg0, object arg1, object arg2);
        void WriteErrorLine(string format, object arg0, object arg1, object arg2, object arg3);
        void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6);
        void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5);
        void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7);

        void WriteWarning(string message);
    }
}
