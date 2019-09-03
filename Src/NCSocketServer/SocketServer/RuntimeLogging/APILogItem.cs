using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.SocketServer.RuntimeLogging
{
    /// <summary>
    /// Stores common fields relevant to NCMonitor logging
    /// </summary>
    public class APILogItem
    {
        /// <summary>
        /// name of command that is executed
        /// </summary>
        internal string CommandName { get; set; }

        /// <summary>
        /// total time of execution
        /// </summary>
        internal TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// Process ID of CacheHost being used by cache
        /// </summary>
        internal string ClientId { get; set; }

        /// <summary>
        /// IP Address from which cache is accessed
        /// </summary>
        internal string ClientIp { get; set; }

        /// <summary>
        /// Message of any exception that has occurred
        /// </summary>
        internal string ExceptionMessage { get; set; }

        [Obsolete("Method Overloads are no longer used. Default value is 1. Only specify if xml for the overload number exists.")]
        /// <summary>
        /// Number of overloads the method has. XML is selected on the basis of Overload.
        /// </summary>
        internal int MethodOverload { get; set; }
        /// <summary>
        /// Takes additional parameters to be displayed in monitor. If any key already exists in the logging hashtable, the value over here is discarded
        /// </summary>
        internal Hashtable AdditionalParameters { get; set; }

        /// <summary>
        /// Constructor initializes parts of LogItem.
        /// </summary>
        public APILogItem()
        {
            CommandName = null;
            ExecutionTime = new TimeSpan();
            ClientId = null;
            ClientIp = null;
            MethodOverload = 1;
            ExceptionMessage = null;
            AdditionalParameters = new Hashtable();
        }
    }
}
