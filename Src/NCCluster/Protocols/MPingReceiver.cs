using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Serialization.Formatters;
#if NETCORE
using System.Runtime.InteropServices;
#endif

namespace Alachisoft.NGroups.Protocols
{
    internal class MPingReceiver : IThreadRunnable
    {
        private TCPPING enclosingInstance;
        private System.Net.Sockets.Socket mcast_recv_sock;
        internal int ip_ttl = 32;
        private Address mcast_addr;
        int buff_size = 64000;
        bool discard_incompatible_packets = true;
        internal ThreadClass mcast_receiver = null;

        public MPingReceiver(TCPPING enclosingInstance)
        {
            this.enclosingInstance = enclosingInstance;
        }
        public TCPPING Enclosing_Instance
        {
            get { return enclosingInstance; }
        }

        private void createSockets()
        {
            IPAddress tmp_addr = null;
            tmp_addr = Address.Resolve(Enclosing_Instance.discovery_addr);
            mcast_addr = new Address(tmp_addr, Enclosing_Instance.discovery_port);

            try
            {
                IPEndPoint recvEndPoint = new IPEndPoint(IPAddress.Any, mcast_addr.Port);
                mcast_recv_sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                mcast_recv_sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                mcast_recv_sock.Bind(recvEndPoint);
                mcast_recv_sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(mcast_addr.IpAddress, IPAddress.Any));
            }
            catch (Exception ex)
            {
                throw ex;
            }
            setBufferSizes();
        }
        private void setBufferSizes()
        {
            if (mcast_recv_sock != null)
            {
                try
                {
                    mcast_recv_sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, buff_size);
                }
                catch (System.Exception ex)
                {
                    enclosingInstance.Stack.NCacheLog.Warn("failed setting mcast_send_buf_size in mcast_recv_sock: " + ex);
                }

                try
                {
                    mcast_recv_sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, buff_size);
                }
                catch (System.Exception ex)
                {
                    enclosingInstance.Stack.NCacheLog.Warn("failed setting mcast_recv_buf_size in mcast_recv_sock: " + ex);
                }
            }
        }

#region IThreadRunnable Members

        public void Run()
        {
            int len;
            byte[] packet = new byte[buff_size];
            EndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

            while (mcast_receiver != null && mcast_recv_sock != null)
            {
                try
                {
                    len = mcast_recv_sock.ReceiveFrom(packet, ref remoteIpEndPoint);

                    if (len == 1 && packet[0] == 0)
                    {
                        if (enclosingInstance.Stack.NCacheLog.IsInfoEnabled) enclosingInstance.Stack.NCacheLog.Info("UDP.Run()", "received dummy packet");
                        continue;
                    }

                    if (len > packet.Length)
                    {
                        enclosingInstance.Stack.NCacheLog.Error("UDP.Run()", "size of the received packet (" + len + ") is bigger than " + "allocated buffer (" + packet.Length + "): will not be able to handle packet. " + "Use the FRAG protocol and make its frag_size lower than " + packet.Length);
                    }

                    if (Version.CompareTo(packet) == false)
                    {
                        if (discard_incompatible_packets)
                            continue;
                    }

                    handleIncomingPacket(packet, len);
                }
                catch (SocketException sock_ex)
                {
                    enclosingInstance.Stack.NCacheLog.Error("MPingReceiver.Run()", "multicast socket is closed, exception=" + sock_ex);
                    break;
                }
                catch (IOException ex)
                {
                    enclosingInstance.Stack.NCacheLog.Error("MPingReceiver.Run()", "exception=" + ex);
                    // thread was interrupted
                    ; // go back to top of loop, where we will terminate loop
                }
                catch (System.Exception ex)
                {
                    enclosingInstance.Stack.NCacheLog.Error("MPingReceiver.Run()", "exception=" + ex + ", stack trace=" + ex.StackTrace);
                    Util.Util.sleep(200); // so we don't get into 100% cpu spinning (should NEVER happen !)
                }
            }
        }

#endregion
        internal bool start()
        {
            try
            {
                createSockets();
                if (mcast_receiver == null)
                {
                    mcast_receiver = new ThreadClass(new System.Threading.ThreadStart(this.Run), "MPingReceiver thread");
                    mcast_receiver.IsBackground = true;
                    mcast_receiver.Start();
                }
            }
            catch (Exception e)
            {
                enclosingInstance.Stack.NCacheLog.Error("MPingReceiver.start()", e.ToString());
                return (false);
            }
            return (true);
        }

        internal void stop()
        {
            mcast_receiver = null;
#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                mcast_recv_sock.Shutdown(SocketShutdown.Both);
            }   
#endif
            mcast_recv_sock.Close();
            mcast_recv_sock = null;
        }
        private void handleIncomingPacket(byte[] data, int dataLen)
        {
            MemoryStream inp_stream;
            Message msg = null;

            try
            {
                inp_stream = new MemoryStream(data, Version.Length, dataLen - Version.Length);
                msg = CompactBinaryFormatter.Deserialize(inp_stream, null) as Message;
                if (msg != null)
                    this.Enclosing_Instance.up(new Event(Event.MSG, (object)msg));
            }
            catch (System.Exception e)
            {
                enclosingInstance.Stack.NCacheLog.Error("MpingReceiver.handleIncomingPacket()", "exception=" + e.ToString() + "\n");
            }
        }
    }
}