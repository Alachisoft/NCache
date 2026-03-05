// GET v1 message handler class.
// Copyright (C) 2009-2010 Lex Li
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using Lextm.SharpSnmpLib.Messaging;

namespace Lextm.SharpSnmpLib.Pipeline
{
    /// <summary>
    /// GET message handler.
    /// </summary>
    /// <remarks>
    /// Follows RFC 1157, 4.1.2
    /// </remarks>
    public sealed class GetV1MessageHandler : IMessageHandler
    {
        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="store">The object store.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void Handle(ISnmpContext context, ObjectStore store)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }  
            
            var status = ErrorCode.NoError;
            var index = 0;
            IList<Variable> result = new List<Variable>();
            foreach (var v in context.Request.Pdu().Variables)
            {
                index++;
                var obj = store.GetObject(v.Id);
                if (obj != null)
                {
                    try
                    {
                        var item = obj.Variable;
                        result.Add(item);
                    }
                    catch (AccessFailureException)
                    {
                        status = ErrorCode.NoSuchName;
                    }
                    catch (Exception)
                    {
                        context.CopyRequest(ErrorCode.GenError, index);
                        return;
                    }
                }
                else
                {
                    status = ErrorCode.NoSuchName;
                }

                if (status == ErrorCode.NoError)
                {
                    continue;
                }

                context.CopyRequest(status, index);
                return;
            }

            context.GenerateResponse(result);
        }
    }
}