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
    
using System;
using System.Reflection;
using System.Text;

using log4net.Appender;
using log4net.Core;

namespace Lextm.Common
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Indentable")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Appender")]
    public class IndentableFileAppender : FileAppender
    {
        private const string EnterMark = "-->";
        private const string LeaveMark = "<--";
        private int _indentLevel;
        
        protected override void Append(LoggingEvent loggingEvent)
        {
            var newData = loggingEvent.GetLoggingEventData();
            newData.Message = Decorate(newData.Message);
            loggingEvent.GetType()
                .GetField("m_data", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(newData, loggingEvent);
            base.Append(loggingEvent);
        }
        
        private string Decorate(string message)
        {
            if (message.StartsWith(EnterMark, StringComparison.Ordinal))
            {
                string result = AppendIndentationTo(message);
                _indentLevel++;
                return result;
            }
            
            if (message.StartsWith(LeaveMark, StringComparison.Ordinal))
            {
                _indentLevel--;
                return AppendIndentationTo(message);
            }
            
            return AppendIndentationTo(message);
        }
        
        private string AppendIndentationTo(string message) 
        {
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < _indentLevel; i++)
            {
                text.Append("    ");
            }
            
            return text.Append(message).ToString();
        }
    }
}