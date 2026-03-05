// Access failure exception class.
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

/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 11/28/2009
 * Time: 7:41 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Runtime.Serialization;

namespace Lextm.SharpSnmpLib.Pipeline
{
    /// <summary>
    /// Access failure exception. 
    /// Raised when,
    /// 1. GET operation is performed on a write-only object.
    /// 2. SET operation is performed on a read-only object.
    /// </summary>
    [Serializable]
    public sealed class AccessFailureException : Exception
    {
        /// <summary>
        /// Creates a <see cref="AccessFailureException"/>.
        /// </summary>
        public AccessFailureException() 
        { 
        }
        
        /// <summary>
        /// Creates a <see cref="AccessFailureException"/> instance with a specific <see cref="String"/>.
        /// </summary>
        /// <param name="message">Message</param>
        public AccessFailureException(string message) : base(message) 
        {
        }
        
        /// <summary>
        /// Creates a <see cref="AccessFailureException"/> instance with a specific <see cref="String"/> and an <see cref="Exception"/>.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="inner">Inner exception</param>
        public AccessFailureException(string message, Exception inner) 
            : base(message, inner) 
        {
        }

#if (!SILVERLIGHT)
        /// <summary>
        /// Creates a <see cref="AccessFailureException"/> instance.
        /// </summary>
        /// <param name="info">Info</param>
        /// <param name="context">Context</param>
        private AccessFailureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        } 
#endif
       
        /// <summary>
        /// Returns a <see cref="String"/> that represents this <see cref="AccessFailureException"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "AccessFailureException: " + Message;
        }
    }
}
