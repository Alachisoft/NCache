//  Copyright (c) 2018 Alachisoft
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using Alachisoft.NCache.Common.ErrorHandling;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.SocketServer.Resp.Protocol;

namespace Alachisoft.NCache.SocketServer.Resp
{
    /// <summary>
    /// Translates NCache exceptions into RESP error replies.
    /// <para>
    /// The error code prefix is not cosmetic: client libraries branch on it. StackExchange.Redis and
    /// redis-py treat <c>LOADING</c> and <c>TRYAGAIN</c> as retryable and <c>OOM</c> as a capacity
    /// signal, so mapping those correctly is the difference between a client backing off sensibly
    /// and a client giving up.
    /// </para>
    /// <para>
    /// Two rules hold everywhere here:
    /// </para>
    /// <list type="number">
    /// <item>Never put a stack trace or internal type name on the wire. The protobuf path ships
    /// <c>exc.ToString()</c> to its clients, which is fine for a trusted typed client but not for a
    /// port anyone can connect to. Detail goes to the log; the client gets the message.</item>
    /// <item>Never let CR or LF reach the reply. <see cref="RespWriter"/> strips them, but messages
    /// are shortened here too so a reply cannot echo back a whole value or a file path.</item>
    /// </list>
    /// </summary>
    internal static class RespErrorMapper
    {
        private const int MaxMessageLength = 200;

        /// <summary>Thrown by the gateway when the cache is not usable yet.</summary>
        internal sealed class CacheUnavailableException : Exception
        {
            public CacheUnavailableException(string message) : base(message) { }
        }

        public static void WriteError(RespWriter writer, Exception exception)
        {
            writer.WriteError(Describe(exception));
        }

        public static string Describe(Exception exception)
        {
            if (exception == null) return "ERR internal error";

            if (exception is CacheUnavailableException)
                return "LOADING " + Shorten(exception.Message);

            if (exception is MaintenanceException)
                return "LOADING NCache is in maintenance mode";

            if (exception is StateTransferInProgressException)
                return "TRYAGAIN state transfer in progress, retry";

            var operationFailed = exception as OperationFailedException;
            if (operationFailed != null)
            {
                switch (operationFailed.ErrorCode)
                {
                    case ErrorCodes.Common.NOT_ENOUGH_ITEMS_EVICTED:
                        return "OOM command not allowed when used memory > 'maxmemory'.";

                    case ErrorCodes.BasicCacheOperations.KEY_ALREADY_EXISTS:
                        // Callers that care about this - SETNX, SET NX - handle it as a normal
                        // result before ever reaching here. Reaching here means some other command
                        // hit it, so report it plainly.
                        return "ERR key already exists";
                }
                return "ERR " + Shorten(operationFailed.Message);
            }

            if (exception is LockingException)
                return "ERR " + Shorten(exception.Message);

            if (exception is SecurityException)
                return "NOPERM this user has no permissions to run this command";

            if (exception is OperationNotSupportedException)
                return "ERR " + Shorten(exception.Message);

            if (exception is ConfigurationException)
                return "ERR configuration error: " + Shorten(exception.Message);

            if (exception is Runtime.Exceptions.TimeoutException)
                return "ERR operation timed out";

            if (exception is ArgumentException)
                return "ERR " + Shorten(exception.Message);

            // Deliberately opaque: an unexpected exception may carry internals that should not
            // leave the process. The caller logs the full detail.
            return "ERR internal error";
        }

        /// <summary>
        /// Trims a message to one short single line. NCache messages routinely carry paths, inner
        /// exception text and newlines; none of that belongs in a RESP error.
        /// </summary>
        private static string Shorten(string message)
        {
            if (string.IsNullOrEmpty(message)) return "operation failed";

            message = message.Replace('\r', ' ').Replace('\n', ' ').Trim();

            if (message.Length > MaxMessageLength)
                message = message.Substring(0, MaxMessageLength).TrimEnd() + "...";

            return message.Length == 0 ? "operation failed" : message;
        }
    }
}
