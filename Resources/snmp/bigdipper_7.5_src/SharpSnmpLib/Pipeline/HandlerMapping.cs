// Handler mapping class.
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
using System.Linq;
using Lextm.SharpSnmpLib.Messaging;

namespace Lextm.SharpSnmpLib.Pipeline
{
    /// <summary>
    /// Handler mapping class, who is used to map incoming messages to their handlers.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    public sealed class HandlerMapping
    {
        private readonly string[] _version;
        private readonly bool _catchAll;
        private readonly string _command;
        private readonly IMessageHandler _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandlerMapping"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="command">The command.</param>
        /// <param name="handler">The handler.</param>
        public HandlerMapping(string version, string command, IMessageHandler handler)
        {
            if (version == null)
            {
                throw new ArgumentNullException("version");
            }

            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            _catchAll = version == "*";
            _version = _catchAll ? new string[0] : version.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            _command = command;
            _handler = handler;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HandlerMapping"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="command">The command.</param>
        /// <param name="type">The type.</param>
        /// <param name="assembly">The assembly.</param>
        public HandlerMapping(string version, string command, string type, string assembly)
        {
            if (version == null)
            {
                throw new ArgumentNullException("version");
            }

            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            _catchAll = version == "*";
            _version = _catchAll ? new string[0] : version.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            _command = command;
            _handler = CreateMessageHandler(assembly, type);
        }

        private static IMessageHandler CreateMessageHandler(string assemblyName, string type)
        {
            foreach (var assembly in from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                          let name = assembly.GetName().Name
                                          where string.Compare(name, assemblyName, StringComparison.OrdinalIgnoreCase) == 0
                                          select assembly)
            {
                return (IMessageHandler)Activator.CreateInstance(assembly.GetType(type));
            }

            return (IMessageHandler)Activator.CreateInstance(AppDomain.CurrentDomain.Load(assemblyName).GetType(type));
        }

        /// <summary>
        /// Gets the handler.
        /// </summary>
        /// <value>The handler.</value>
        public IMessageHandler Handler
        {
            get { return _handler; }
        }

        /// <summary>
        /// Determines whether this instance can handle the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>
        ///     <c>true</c> if this instance can handle the specified message; otherwise, <c>false</c>.
        /// </returns>
        public bool CanHandle(ISnmpMessage message)
        {
            return VersionMatched(message) && CommandMatched(message);
        }

        private bool CommandMatched(ISnmpMessage message)
        {
            var codeString = message.Pdu().TypeCode.ToString();
            return StringEquals(_command, "*") || StringEquals(_command + "RequestPdu", codeString) ||
            StringEquals(_command + "Pdu", codeString);
        }

        private bool VersionMatched(ISnmpMessage message)
        {
            return _catchAll || _version.Any(v => StringEquals(message.Version.ToString(), v));
        }

        private static bool StringEquals(string left, string right)
        {
            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}