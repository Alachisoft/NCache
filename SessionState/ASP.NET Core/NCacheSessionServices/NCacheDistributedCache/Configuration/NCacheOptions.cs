using Alachisoft.NCache.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Caching.Distributed.Configuration
{
    public class NCacheConfiguration
    {
        /// <summary>
        /// Mandatory name of the cache
        /// </summary>
        public string CacheName { get; set; }

        /// <summary>
        /// Gets/Sets list of servers provided by user
        /// </summary>
        public IList<ServerInfo> ServerList { get; set; }

        /// <summary>
        /// ID of Default Read Thru Provider
        /// </summary>
        public string DefaultReadThruProvider { get; set; }

        /// <summary>
        /// Gets/Sets the IP for the client to be binded with
        /// </summary>
        public string BindIP { get; set; }

        /// <summary>
        /// Gets/Sets ID of DefaultWriteThruProvider
        /// </summary>
        public string DefaultWriteThruProvider { get; set; }

        /// <summary>
        /// Gets/Sets the cache mode (inproc/outproc)
        /// </summary>
        public IsolationLevel? Mode { get; set; }

        /// <summary>
        /// When this flag is set, client tries to connect to the optimum server in terms of number of connected clients.
        /// This way almost equal number of clients are connected to every node in the clustered cache and no single node 
        /// is overburdened.
        /// </summary>
        public bool? LoadBalance { get; set; }

        /// <summary>
        /// Clients operation timeout specified in seconds.
        /// Clients wait for the response from the server for this time. 
        /// If the response is not received within this time, the operation is not successful.
        /// Based on the network conditions, OperationTimeout value can be adjusted. 
        /// The default value is 90 seconds.
        /// </summary>
        public TimeSpan? ClientRequestTimeOut { get; set; }

        /// <summary>
        /// Client's connection timeout specified in seconds.
        /// </summary>
        public TimeSpan? ConnectionTimeout { get; set; }

        /// <summary>
        /// Number of tries to re-establish a broken connection between client and server.
        /// </summary>
        public int? ConnectionRetries { get; set; }

        /// <summary>
        /// Time in seconds to wait between two connection retries.
        /// </summary>
        public TimeSpan? RetryInterval { get; set; }

        /// <summary>
        /// The time after which client will try to reconnect to the server.
        /// </summary>
        public TimeSpan? RetryConnectionDelay { get; set; }

        /// <summary>
        /// If client application sends request to server for any operation and a response is not received, then the number of retries it will make until it gets response is defined here.
        /// </summary>
        public int? CommandRetries { get; set; }

        /// <summary>
        /// In case if client app doesn’t get response against some operation call on server, the command retry interval defines the waiting period before the next attempt to send the operation the server is made.
        /// Type integer which defines seconds.
        /// </summary>
        public TimeSpan? CommandRetryInterval { get; set; }

        /// <summary>
        /// Sets the keep alive flag
        /// </summary>
        public bool? EnableKeepAlive { get; set; }

        /// <summary>
        /// Gets or Sets the KeepAliveInterval, which will be in effect if the EnabledKeepAlive is set 'true' or is specified 'true' from the client configuration.
        /// Note: If the value to be set is lesser than 1 or is greater than 7200 (2 hours in seconds), it will resort back 30 seconds internally.
        /// </summary>
        public TimeSpan? KeepAliveInterval { get; set; }

        /// <summary>
        /// If different client applications are connected to server and because of any issue which results in connection failure with server, after the client again establishes connection “AppName” is used to identify these different client applications.
        ///Data type is string. Its optional.If value is not set it takes the value of the process id.
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// Time after which a new request forcefully releases the lock which is being held by an older request
        /// </summary>
        public double? RequestTimeout { get; set; }

        /// <summary>
        /// Should any exceptions that occured in the session state module be reported to event log
        /// </summary>
        public bool? WriteExceptionsToEventLog { get; set; }

        /// <summary>
        /// Should NCache logs be enabled
        /// </summary>
        public bool? EnableLogs { get; set; }

        /// <summary>
        /// Should NCache detailed logs be enabled
        /// </summary>
        public bool? EnableDetailLogs { get; set; }

        /// <summary>
        /// Should exceptions be thrown to the application if they occur
        /// </summary>
        public bool? ExceptionsEnabled { get; set; }

    }

}
