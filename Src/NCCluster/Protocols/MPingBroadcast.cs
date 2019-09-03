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
    internal class MPingBroadcast
    {
        private TCPPING enclosingInstance;
        private System.Net.Sockets.Socket mcast_send_sock;
        internal int ip_ttl = 32;
        private Address mcast_addr;
        int buff_size = 64000;

        public MPingBroadcast(TCPPING enclosingInstance)
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
                IPEndPoint sendEndPoint = new IPEndPoint(mcast_addr.IpAddress, mcast_addr.Port);
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 0);

                mcast_send_sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                mcast_send_sock.Bind(localEndPoint);
                mcast_send_sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(mcast_addr.IpAddress, IPAddress.Any));
                mcast_send_sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, ip_ttl);
                mcast_send_sock.Connect(sendEndPoint);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            setBufferSizes();
        }
        public void send(Message msg)
        {
            msg.Src = Enclosing_Instance.local_addr;
            byte[] buf = messageToBuffer(msg);
            if (mcast_send_sock != null)
            {
                mcast_send_sock.Send(buf);
            }
        }
        public bool start()
        {
            try
            {
                createSockets();
            }
            catch (Exception e)
            {
                enclosingInstance.Stack.NCacheLog.Error("MPingBroadcast.start()", e.ToString());
                return false;
            }
            if (enclosingInstance.Stack.NCacheLog.IsInfoEnabled) enclosingInstance.Stack.NCacheLog.Info("MPingBroadcast.Start()", " multicast sockets created successfully");
            return true;
        }
        public void stop()
        {
            if (mcast_send_sock != null)
            {
#if NETCORE
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    mcast_send_sock.Shutdown(SocketShutdown.Both);
                }
#endif
                mcast_send_sock.Close();
                mcast_send_sock = null;
            }
        }


        private byte[] messageToBuffer(Message msg)
        {
            using (MemoryStream s = new MemoryStream())
            {
                s.Write(Version.version_id, 0, Version.version_id.Length);
                CompactBinaryFormatter.Serialize(s, msg, null);

                byte[] buffer = s.ToArray();
                s.Close();

                return buffer;
            }
        }
        private void setBufferSizes()
        {
            if (mcast_send_sock != null)
            {
                try
                {
                    mcast_send_sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, buff_size);
                }
                catch (System.Exception ex)
                {
                    enclosingInstance.Stack.NCacheLog.Warn("failed setting mcast_send_buf_size in mcast_send_sock: " + ex);
                }

                try
                {
                    mcast_send_sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, buff_size);
                }
                catch (System.Exception ex)
                {
                    enclosingInstance.Stack.NCacheLog.Warn("failed setting mcast_recv_buf_size in mcast_send_sock: " + ex);
                }
            }
        }
    }
}