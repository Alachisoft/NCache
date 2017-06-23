
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Resources;
using System.Text;
using System.Threading;

using log4net;


namespace Memcached.ClientLibrary
{
    /// <summary>
    /// Memcached C# client, utility class for Socket IO.
    /// 
    /// This class is a wrapper around a Socket and its streams.
    /// </summary>
    public class SockIO
    {
        // logger
        private static ILog Log = LogManager.GetLogger(typeof(SockIO));

        // id generator.  Gives ids to all the SockIO instances
        private static int IdGenerator;

        // id
        private int _id;

        // time created
        private DateTime _created;

        // pool
        private SockIOPool _pool;

        // data
        private String _host;
        private Socket _socket;
        private Stream _networkStream;

        private SockIO()
        {
            _id = Interlocked.Increment(ref IdGenerator);
            _created = DateTime.Now;
        }

        /// <summary>
        /// creates a new SockIO object wrapping a socket
        /// connection to host:port, and its input and output streams
        /// </summary>
        /// <param name="pool">Pool this object is tied to</param>
        /// <param name="host">host to connect to</param>
        /// <param name="port">port to connect to</param>
        /// <param name="timeout">int ms to block on data for read</param>
        /// <param name="connectTimeout">timeout (in ms) for initial connection</param>
        /// <param name="noDelay">TCP NODELAY option?</param>
        public SockIO(SockIOPool pool, String host, int port, int timeout, int connectTimeout, bool noDelay)
            : this()
        {
            if (host == null || host.Length == 0)
                throw new ArgumentNullException(GetLocalizedString("host"), GetLocalizedString("null host"));

            _pool = pool;


            if (connectTimeout > 0)
            {
                _socket = GetSocket(host, port, connectTimeout);
            }
            else
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Connect(new IPEndPoint(IPAddress.Parse(host), port));
            }

            _networkStream = new BufferedStream(new NetworkStreamIgnoreSeek(_socket));

            _host = host + ":" + port;
        }

        /// <summary>
        /// creates a new SockIO object wrapping a socket
        /// connection to host:port, and its input and output streams
        /// </summary>
        /// <param name="pool">Pool this object is tied to</param>
        /// <param name="host">hostname:port</param>
        /// <param name="timeout">read timeout value for connected socket</param>
        /// <param name="connectTimeout">timeout for initial connections</param>
        /// <param name="noDelay">TCP NODELAY option?</param>
        public SockIO(SockIOPool pool, String host, int timeout, int connectTimeout, bool noDelay)
            : this()
        {
            if (host == null || host.Length == 0)
                throw new ArgumentNullException(GetLocalizedString("host"), GetLocalizedString("null host"));

            _pool = pool;


            String[] ip = host.Split(':');

            // get socket: default is to use non-blocking connect
            if (connectTimeout > 0)
            {
                _socket = GetSocket(ip[0], int.Parse(ip[1], new System.Globalization.NumberFormatInfo()), connectTimeout);
            }
            else
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Connect(new IPEndPoint(IPAddress.Parse(ip[0]), int.Parse(ip[1], new System.Globalization.NumberFormatInfo())));
            }

            _networkStream = new BufferedStream(new NetworkStreamIgnoreSeek(_socket));

            _host = host;
        }

        /// <summary>
        /// Method which spawns thread to get a connection and then enforces a timeout on the initial
        /// connection.
        /// 
        /// This should be backed by a thread pool.  Any volunteers?
        /// </summary>
        /// <param name="host">host to establish connection to</param>
        /// <param name="port">port on that host</param>
        /// <param name="timeout">connection timeout in ms</param>
        /// <returns>connected socket</returns>
        protected static Socket GetSocket(String host, int port, int timeout)
        {
            // Create a new thread which will attempt to connect to host:port, and start it running
            ConnectThread thread = new ConnectThread(host, port);
            thread.Start();

            int timer = 0;
            int sleep = 25;

            while (timer < timeout)
            {

                // if the thread has a connected socket
                // then return it
                if (thread.IsConnected)
                    return thread.Socket;

                // if the thread had an error
                // then throw a new IOException
                if (thread.IsError)
                    throw new IOException();

                try
                {
                    // sleep for short time before polling again
                    Thread.Sleep(sleep);
                }
                catch (ThreadInterruptedException) { }

                // Increment timer
                timer += sleep;
            }

            // made it through loop without getting connection
            // the connection thread will timeout on its own at OS timeout
            throw new IOException(GetLocalizedString("connect timeout").Replace("$$timeout$$", timeout.ToString(new System.Globalization.NumberFormatInfo())));
        }

        /// <summary>
        /// returns the host this socket is connected to 
        /// 
        /// String representation of host (hostname:port)
        /// </summary>
        public string Host
        {
            get { return _host; }
        }

        /// <summary>
        /// closes socket and all streams connected to it 
        /// </summary>
        public void TrueClose()
        {
			if(Log.IsDebugEnabled)
			{
				Log.Debug(GetLocalizedString("true close socket").Replace("$$Socket$$", ToString()).Replace("$$Lifespan$$", DateTime.Now.Subtract(_created).ToString()));
			}

            bool err = false;
            StringBuilder errMsg = new StringBuilder();

            if (_socket == null || _networkStream == null)
            {
                err = true;
                errMsg.Append(GetLocalizedString("socket already closed"));
            }

            if (_socket != null)
            {
                try
                {
                    _socket.Close();
                }
                catch (IOException ioe)
                {
					if(Log.IsErrorEnabled)
					{
						Log.Error(GetLocalizedString("error closing socket").Replace("$$ToString$$", ToString()).Replace("$$Host$$", Host), ioe);
					}
                    errMsg.Append(GetLocalizedString("error closing socket").Replace("$$ToString$$", ToString()).Replace("$$Host$$", Host) + System.Environment.NewLine);
                    errMsg.Append(ioe.ToString());
                    err = true;
                }
                catch (SocketException soe)
                {
					if(Log.IsErrorEnabled)
					{
						Log.Error(GetLocalizedString("error closing socket").Replace("$$ToString$$", ToString()).Replace("$$Host$$", Host), soe);
					}
                    errMsg.Append(GetLocalizedString("error closing socket").Replace("$$ToString$$", ToString()).Replace("$$Host$$", Host) + System.Environment.NewLine);
                    errMsg.Append(soe.ToString());
                    err = true;
                }
            }

            // check in to pool
            if (_socket != null)
                _pool.CheckIn(this, false);

            _networkStream = null;
            _socket = null;

            if (err)
                throw new IOException(errMsg.ToString());
        }

        /// <summary>
        /// sets closed flag and checks in to connection pool
        /// but does not close connections
        /// </summary>
        public void Close()
        {
            // check in to pool
			if(Log.IsDebugEnabled)
			{
				Log.Debug(GetLocalizedString("close socket").Replace("$$ToString$$", ToString()));
			}
            _pool.CheckIn(this);
        }

        /// <summary>
        /// Gets whether or not the socket is connected.  Returns <c>true</c> if it is.
        /// </summary>
        public bool IsConnected
        {
            get { return _socket != null && _socket.Connected; }
        }

        /// <summary>
        /// reads a line
        /// intentionally not using the deprecated readLine method from DataInputStream 
        /// </summary>
        /// <returns>String that was read in</returns>
        public string ReadLine()
        {
            if (_socket == null || !_socket.Connected)
            {
				if(Log.IsErrorEnabled)
				{
					Log.Error(GetLocalizedString("read closed socket"));
				}
                throw new IOException(GetLocalizedString("read closed socket"));
            }

            byte[] b = new byte[1];
            MemoryStream memoryStream = new MemoryStream();
            bool eol = false;

            while (_networkStream.Read(b, 0, 1) != -1)
            {

                if (b[0] == 13)
                {
                    eol = true;

                }
                else
                {
                    if (eol)
                    {
                        if (b[0] == 10)
                            break;

                        eol = false;
                    }
                }

                // cast byte into char array
                memoryStream.Write(b, 0, 1);
            }

            if (memoryStream == null || memoryStream.Length <= 0)
            {
                throw new IOException(GetLocalizedString("closing dead stream"));
            }

            // else return the string
            string temp = UTF8Encoding.UTF8.GetString(memoryStream.GetBuffer()).TrimEnd('\0', '\r', '\n');
            return temp;
        }

        /// <summary>
        /// reads up to end of line and returns nothing 
        /// </summary>
        public void ClearEndOfLine()
        {
            if (_socket == null || !_socket.Connected)
            {
                Log.Error(GetLocalizedString("read closed socket"));
                throw new IOException(GetLocalizedString("read closed socket"));
            }

            byte[] b = new byte[1];
            bool eol = false;
            while (_networkStream.Read(b, 0, 1) != -1)
            {

                // only stop when we see
                // \r (13) followed by \n (10)
                if (b[0] == 13)
                {
                    eol = true;
                    continue;
                }

                if (eol)
                {
                    if (b[0] == 10)
                        break;

                    eol = false;
                }
            }
        }

        /// <summary>
        /// reads length bytes into the passed in byte array from stream
        /// </summary>
        /// <param name="b">byte array</param>
        public void Read(byte[] bytes)
        {
            if (_socket == null || !_socket.Connected)
            {
				if(Log.IsErrorEnabled)
				{
					Log.Error(GetLocalizedString("read closed socket"));
				}
                throw new IOException(GetLocalizedString("read closed socket"));
            }

            if (bytes == null)
                return;

            int count = 0;
            while (count < bytes.Length)
            {
                int cnt = _networkStream.Read(bytes, count, (bytes.Length - count));
                count += cnt;
            }
        }

        /// <summary>
        /// flushes output stream 
        /// </summary>
        public void Flush()
        {
            if (_socket == null || !_socket.Connected)
            {
				if(Log.IsErrorEnabled)
				{
					Log.Error(GetLocalizedString("write closed socket"));
				}
                throw new IOException(GetLocalizedString("write closed socket"));
            }
            _networkStream.Flush();
        }

        /// <summary>
        /// writes a byte array to the output stream
        /// </summary>
        /// <param name="bytes">byte array to write</param>
        public void Write(byte[] bytes)
        {
			Write(bytes, 0, bytes.Length);
        }

		/// <summary>
		/// writes a byte array to the output stream
		/// </summary>
		/// <param name="bytes">byte array to write</param>
		/// <param name="offset">offset to begin writing from</param>
		/// <param name="count">count of bytes to write</param>
		public void Write(byte[] bytes, int offset, int count)
		{
			if (_socket == null || !_socket.Connected)
			{
				if(Log.IsErrorEnabled)
				{
					Log.Error(GetLocalizedString("write closed socket"));
				}
				throw new IOException(GetLocalizedString("write closed socket"));
			}
			if (bytes != null)
				_networkStream.Write(bytes, offset, count);
		}
		/// <summary>
        /// returns the string representation of this socket 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (_socket == null)
                return "";
            return _id.ToString(new System.Globalization.NumberFormatInfo());
        }

        /// <summary>
        /// Thread to attempt connection. 
        /// This will be polled by the main thread. We run the risk of filling up w/
        /// threads attempting connections if network is down.  However, the falling off
        /// mech in the main code should limit this.
        /// </summary>
        private class ConnectThread
        {
            //thread
            Thread _thread;

            // logger
            private static ILog Log = LogManager.GetLogger(typeof(ConnectThread));

            private Socket _socket;
            private String _host;
            private int _port;
            bool _error;

            /// <summary>
            /// Constructor 
            /// </summary>
            /// <param name="host"></param>
            /// <param name="port"></param>
            public ConnectThread(string host, int port)
            {
                _host = host;
                _port = port;

                _thread = new Thread(new ThreadStart(Connect));
                _thread.IsBackground = true;
            }

            /// <summary>
            /// The logic of the thread.
            /// </summary>
            private void Connect()
            {
                try
                {
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    _socket.Connect(new IPEndPoint(IPAddress.Parse(_host), _port));
                }
                catch (IOException)
                {
                    _error = true;
                }
                catch (SocketException ex)
                {
                    _error = true;
					if(Log.IsDebugEnabled)
					{
						Log.Debug(GetLocalizedString("socket connection exception"), ex);
					}
                }

				if(Log.IsDebugEnabled)
				{
					Log.Debug(GetLocalizedString("connect thread connect").Replace("$$Host$$", _host));
				}
            }

            /// <summary>
            /// start thread running.
            /// This attempts to establish a connection. 
            /// </summary>
            public void Start()
            {
                _thread.Start();
            }

            /// <summary>
            /// Is the new socket connected yet 
            /// </summary>
            public bool IsConnected
            {
                get { return _socket != null && _socket.Connected; }
            }

            /// <summary>
            /// Did we have an exception while connecting? 
            /// </summary>
            /// <returns></returns>
            public bool IsError
            {
                get { return _error; }
            }

            /// <summary>
            /// Return the socket. 
            /// </summary>
            public Socket Socket
            {
                get { return _socket; }
            }
        }

        private static ResourceManager _resourceManager = new ResourceManager("Memcached.ClientLibrary.StringMessages", typeof(SockIO).Assembly);
        private static string GetLocalizedString(string key)
        {
            return _resourceManager.GetString(key);
        }
    }
}
