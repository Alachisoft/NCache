using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alachisoft.NCache.Automation.ToolsOutput
{
    public class CommandLineOutputConsole : IOutputConsole
    {
        public void WriteErrorLine(string message)
        {
            Console.Error.WriteLine(message);
        }

        public void WriteErrorLine(string format, object arg0, object arg1, object arg2)
        {
            Console.Error.WriteLine(format, arg0, arg1, arg2);
        }

        public void WriteErrorLine(string format, object arg0, object arg1, object arg2, object arg3)
        {
            Console.Error.WriteLine(format, arg0, arg1, arg2, arg3);
        }

        public void WriteErrorLine(string format, object arg0, object arg1)
        {
            Console.Error.WriteLine(format, arg0, arg1);
        }

        public void WriteErrorLine(string format, string message)
        {
            Console.Error.WriteLine(format, message);
        }

        public void WriteErrorLine(object message)
        {
            Console.Error.WriteLine(message);
        }

        public void WriteLine(object message)
        {
            Console.WriteLine(message);
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            Console.WriteLine(format, arg0, arg1, arg2);
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2, object arg3)
        {
            Console.WriteLine(format, arg0, arg1, arg2, arg3);
        }

        public void WriteLine(string format, object arg0, object arg1)
        {
            Console.WriteLine(format, arg0, arg1);
        }

        public void WriteLine(string format, string message)
        {
            Console.WriteLine(format, message);
        }

        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteLine(string message, object args0)
        {
            Console.WriteLine(message + args0.ToString());
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8, object arg9)
        {
            Console.WriteLine(format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            Console.WriteLine(format, arg0, arg1, arg2, arg3, arg4);
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
        {
            Console.WriteLine(format, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            Console.WriteLine(format, arg0, arg1, arg2, arg3, arg4, arg5);
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
        {
            Console.WriteLine(format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        public void WriteWarning(string message)
        {
            Console.WriteLine("WARNING: " + message);
        }
    }
}
