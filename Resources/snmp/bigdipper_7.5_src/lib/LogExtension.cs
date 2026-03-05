//Copyright (c) 2010 Lex Li
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this
//software and associated documentation files (the "Software"), to deal in the Software
//without restriction, including without limitation the rights to use, copy, modify, merge,
//publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
//to whom the Software is furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or
//substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
//FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
//OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
//DEALINGS IN THE SOFTWARE.
    
using System.Diagnostics;
using System.Reflection;
using System.Text;

using log4net;

namespace Lextm.Common
{
    public static class LogExtension
    {
        private const string EnterMark = "-->";
        private const string LeaveMark = "<--";
        
        public static void EnterMethod(this ILog logger)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug(GetCallerName(new StringBuilder(EnterMark)));
            }
        }
        
        public static void LeaveMethod(this ILog logger)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug(GetCallerName(new StringBuilder(LeaveMark)));
            }
        }
        
        private static string GetCallerName(StringBuilder result)
        {
            var mi = (new StackTrace(true)).GetFrame(2).GetMethod();
            result.AppendFormat("{0}.{1}(", mi.ReflectedType.Name, mi.Name);
            foreach (var parameter in mi.GetParameters())
            {
                result.AppendFormat(parameter.IsOut ? "out {0}, " : "{0}, ", parameter.ParameterType);
            }
            
            if (mi.GetParameters().Length > 0)
            {
                result.Length = result.Length - 2;
            }
            
            return result.Append(")").ToString();
        }
    }
}