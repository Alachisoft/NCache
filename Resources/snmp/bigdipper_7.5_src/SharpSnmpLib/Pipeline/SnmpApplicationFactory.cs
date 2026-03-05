// SNMP application factory class.
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

using System.Collections.Generic;

namespace Lextm.SharpSnmpLib.Pipeline
{
    /// <summary>
    /// SNMP application factory, who holds all pipeline instances.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    public sealed class SnmpApplicationFactory
    {
        private readonly ILogger _logger;
        private readonly ObjectStore _store;
        private readonly IMembershipProvider _membershipProvider;
        private readonly MessageHandlerFactory _factory;
        private readonly object _root = new object();
        private readonly Queue<SnmpApplication> _queue = new Queue<SnmpApplication>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SnmpApplicationFactory"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="store">The store.</param>
        /// <param name="membershipProvider">The membership provider.</param>
        /// <param name="factory">The factory.</param>
        public SnmpApplicationFactory(ILogger logger, ObjectStore store, IMembershipProvider membershipProvider, MessageHandlerFactory factory)
        {
            _logger = logger;
            _membershipProvider = membershipProvider;
            _store = store;
            _factory = factory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnmpApplicationFactory"/> class.
        /// </summary>
        /// <param name="store">The store.</param>
        /// <param name="membershipProvider">The membership provider.</param>
        /// <param name="factory">The factory.</param>
        public SnmpApplicationFactory(ObjectStore store, IMembershipProvider membershipProvider, MessageHandlerFactory factory)
            : this(null, store, membershipProvider, factory) // TODO: handle the null case in the future.
        {
        }

        /// <summary>
        /// Creates a pipeline for the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public SnmpApplication Create(ISnmpContext context)
        {
            SnmpApplication result = null;
            lock (_root)
            {
                if (_queue.Count > 0)
                {
                    result = _queue.Dequeue();
                }
            }

            if (result == null)
            {
                result = new SnmpApplication(this, _logger, _store, _membershipProvider, _factory);              
            }

            result.Init(context);
            return result;
        }

        /// <summary>
        /// Reuses the specified pipeline.
        /// </summary>
        /// <param name="application">The application.</param>
        internal void Reuse(SnmpApplication application)
        {
            lock (_root)
            {
                _queue.Enqueue(application);
            }
        }
    }
}