// Engine group class.
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

namespace Lextm.SharpSnmpLib.Pipeline
{
    /// <summary>
    /// Engine group which contains all related objects.
    /// </summary>
    public sealed class EngineGroup
    {
        // TODO: make engine ID configurable from outside and unique.
        private readonly OctetString _engineId =
            new OctetString(new byte[] { 128, 0, 31, 136, 128, 233, 99, 0, 0, 214, 31, 244 });

        /// <summary>
        /// Initializes a new instance of the <see cref="EngineGroup"/> class.
        /// </summary>
        public EngineGroup()
        {
            EngineBoots = 0;
        }
        
        /// <summary>
        /// Count of handled REPORT messages.
        /// </summary>
        [CLSCompliant(false)]
        public uint ReportCount { get; set; }

        /// <summary>
        /// Gets the engine id.
        /// </summary>
        /// <value>The engine id.</value>
        internal OctetString EngineId
        {
            get { return _engineId; }
        }

        /// <summary>
        /// Gets or sets the engine boots.
        /// </summary>
        /// <value>The engine boots.</value>
        internal int EngineBoots { get; set; }

        /// <summary>
        /// Gets the engine time.
        /// </summary>
        /// <value>The engine time.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public int EngineTime
        {
            get { return Environment.TickCount; }
        }

        /// <summary>
        /// Verifies if the request comes in time.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="past"></param>
        /// <returns></returns>
        public static bool IsInTime(int current, int past)
        {
            // TODO: make 500 configurable            
            var diff = current > past ? current - past : current - past - int.MinValue + int.MaxValue;
            return diff >= 0 && diff <= 500;
        }
    }
}