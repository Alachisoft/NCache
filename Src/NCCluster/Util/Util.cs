// $Id: Util.java,v 1.13 2004/07/28 08:14:14 belaban Exp $
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Alachisoft.NCache.Common;
using System.Text;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System.Collections;
using Alachisoft.NGroups.Blocks;

namespace Alachisoft.NGroups.Util
{
    /// <summary> Collection of various utility routines that can not be assigned to other classes.</summary>
    internal class Util
	{
		// constants
		public const int MAX_PORT = 65535; // highest port allocatable

		/// <summary>Finds first available port starting at start_port and returns server socket </summary>
		public static System.Net.Sockets.TcpListener createServerSocket(int start_port)
		{
			System.Net.Sockets.TcpListener ret = null;
			
			while (true)
			{
				try
				{
					System.Net.Sockets.TcpListener temp_tcpListener;
					temp_tcpListener = new System.Net.Sockets.TcpListener(start_port);
					temp_tcpListener.Start();
					ret = temp_tcpListener;
				}
				catch (System.Net.Sockets.SocketException bind_ex)
				{
					start_port++;
					continue;
				}
				catch (System.IO.IOException io_ex)
				{
				}
				break;
			}
			return ret;
		}
		

		/// <summary> Returns all members that left between 2 views. All members that are element of old_mbrs but not element of
		/// new_mbrs are returned.
		/// </summary>
		public static System.Collections.ArrayList determineLeftMembers(System.Collections.ArrayList old_mbrs, System.Collections.ArrayList new_mbrs)
		{
			System.Collections.ArrayList retval = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
			object mbr;
			
			if (old_mbrs == null || new_mbrs == null)
				return retval;
			
			for (int i = 0; i < old_mbrs.Count; i++)
			{
				mbr = old_mbrs[i];
				if (!new_mbrs.Contains(mbr))
					retval.Add(mbr);
			}
			
			return retval;
		}
		

		/// <summary>Sleep for timeout msecs. Returns when timeout has elapsed or thread was interrupted </summary>
		public static void  sleep(long timeout)
		{
			System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(timeout));
		}

        internal static IList serializeMessage(Message msg)
        {
            
            int len = 0;
            byte[] buffie;
            FlagsByte flags = new FlagsByte();

            msg.Dest = null;
            msg.Dests = null;

            RequestCorrelator.HDR rqHeader = (RequestCorrelator.HDR)msg.getHeader(HeaderType.REQUEST_COORELATOR);

            if (rqHeader != null)
            {
                rqHeader.serializeFlag = false;
            }

            ClusteredMemoryStream stmOut = new ClusteredMemoryStream();
            stmOut.Write(Util.WriteInt32(len), 0, 4);
            stmOut.Write(Util.WriteInt32(len), 0, 4);

            if (msg.IsUserMsg)
            {
                BinaryWriter msgWriter = new BinaryWriter(stmOut, new UTF8Encoding(true));
                flags.SetOn(FlagsByte.Flag.TRANS);
                msgWriter.Write(flags.DataByte);
                msg.SerializeLocal(msgWriter);
            }
            else
            {
                flags.SetOff(FlagsByte.Flag.TRANS);
                stmOut.WriteByte(flags.DataByte);
                CompactBinaryFormatter.Serialize(stmOut, msg, null, false);
            }

            len = (int)stmOut.Position - 4;

            int payloadLength = 0;
            // the user payload size. payload is passed on untill finally send on the socket.
            if (msg.Payload != null)
            {
                for (int i = 0; i < msg.Payload.Length; i++)
                {
                    payloadLength += ((byte[])msg.Payload.GetValue(i)).Length;
                }
                len += payloadLength;
            }

            stmOut.Position = 0;
            stmOut.Write(Util.WriteInt32(len), 0, 4);
            stmOut.Write(Util.WriteInt32(len - 4 - payloadLength), 0, 4);
            return stmOut.GetInternalBuffer();
        }

		/// <summary> On most UNIX systems, the minimum sleep time is 10-20ms. Even if we specify sleep(1), the thread will
		/// sleep for at least 10-20ms. On Windows, sleep() seems to be implemented as a busy sleep, that is the
		/// thread never relinquishes control and therefore the sleep(x) is exactly x ms long.
		/// </summary>
		public static void  sleep(long msecs, bool busy_sleep)
		{
			if (!busy_sleep)
			{
				sleep(msecs);
				return ;
			}
			
			long start = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
			long stop = start + msecs;
			
			while (stop > start)
			{
				start = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
			}
		}	

		/// <summary>Returns a random value in the range [1 - range] </summary>
		public static long random(long range)
		{
			return (long) ((Global.Random.NextDouble() * 100000) % range) + 1;
		}

		/// <summary>E.g. 2000,4000,8000</summary>
		public static long[] parseCommaDelimitedLongs(string s)
		{
			if (s == null) return null;

			string[] v = s.Split(',');
			if (v.Length == 0) return null;
			
			long[] retval = new long[v.Length];
			for (int i = 0; i < v.Length; i++)
				retval[i] = Convert.ToInt64(v[i].Trim());
			return retval;
		}

		/// <summary> Selects a random subset of members according to subset_percentage and returns them.
		/// Picks no member twice from the same membership. If the percentage is smaller than 1 -> picks 1 member.
		/// </summary>
		public static System.Collections.ArrayList pickSubset(System.Collections.ArrayList members, double subset_percentage)
		{
			System.Collections.ArrayList ret = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10)), tmp_mbrs;
			int num_mbrs = members.Count, subset_size, index;
			
			if (num_mbrs == 0)
				return ret;
			subset_size = (int) System.Math.Ceiling(num_mbrs * subset_percentage);
			
			tmp_mbrs = (System.Collections.ArrayList) members.Clone();
			
			for (int i = subset_size; i > 0 && tmp_mbrs.Count > 0; i--)
			{
				index = (int) ((Global.Random.NextDouble() * num_mbrs) % tmp_mbrs.Count);
				ret.Add(tmp_mbrs[index]);
				tmp_mbrs.RemoveAt(index);
			}
			
			return ret;
		}
		
		/// <summary>Tries to read an object from the message's buffer and prints it </summary>
		public static string printMessage(Message msg)
		{
			if (msg == null)
				return "";
			if (msg.Length == 0)
				return null;
			
			try
			{
				return msg.getObject().ToString();
			}
			catch (System.Exception ex)
			{
				//Trace.error("util.printMessage()",ex.Message);
				// it is not an object
				return "";
			}
		}
		
		public static string printEvent(Event evt)
		{
			Message msg;
			
			if (evt.Type == Event.MSG)
			{
				msg = (Message) evt.Arg;
				if (msg != null)
				{
					if (msg.Length > 0)
						return printMessage(msg);
					else
						return msg.printObjectHeaders();
				}
			}
			return evt.ToString();
		}

		/// <summary>Fragments a byte buffer into smaller fragments of (max.) frag_size.
		/// Example: a byte buffer of 1024 bytes and a frag_size of 248 gives 4 fragments
		/// of 248 bytes each and 1 fragment of 32 bytes.
		/// </summary>
		/// <returns> An array of byte buffers (<code>byte[]</code>).
		/// </returns>
		public static byte[][] fragmentBuffer(byte[] buf, int frag_size)
		{
			byte[][] retval;
			long total_size = buf.Length;
			int accumulated_size = 0;
			byte[] fragment;
			int tmp_size = 0;
			int num_frags;
			int index = 0;
			
			num_frags = buf.Length % frag_size == 0?buf.Length / frag_size:buf.Length / frag_size + 1;
			retval = new byte[num_frags][];
			
			while (accumulated_size < total_size)
			{
				if (accumulated_size + frag_size <= total_size)
					tmp_size = frag_size;
				else
					tmp_size = (int) (total_size - accumulated_size);
				fragment = new byte[tmp_size];
				Array.Copy(buf, accumulated_size, fragment, 0, tmp_size);
				retval[index++] = fragment;
				accumulated_size += tmp_size;
			}
			return retval;
		}
		
		
		/// <summary> Given a buffer and a fragmentation size, compute a list of fragmentation offset/length pairs, and
		/// return them in a list. Example:<br/>
		/// Buffer is 10 bytes, frag_size is 4 bytes. Return value will be ({0,4}, {4,4}, {8,2}).
		/// This is a total of 3 fragments: the first fragment starts at 0, and has a length of 4 bytes, the second fragment
		/// starts at offset 4 and has a length of 4 bytes, and the last fragment starts at offset 8 and has a length
		/// of 2 bytes.
		/// </summary>
		/// <param name="">frag_size
		/// </param>
		/// <returns> List. A List<Range> of offset/length pairs
		/// </returns>
		public static System.Collections.IList computeFragOffsets(int offset, int length, int frag_size)
		{
			System.Collections.IList retval = new System.Collections.ArrayList();
			long total_size = length + offset;
			int index = offset;
			int tmp_size = 0;
			Range r;
			
			while (index < total_size)
			{
				if (index + frag_size <= total_size)
					tmp_size = frag_size;
				else
					tmp_size = (int) (total_size - index);
				r = new Range(index, tmp_size);
				retval.Add(r);
				index += tmp_size;
			}
			return retval;
		}
		
		public static System.Collections.IList computeFragOffsets(byte[] buf, int frag_size)
		{
			return computeFragOffsets(0, buf.Length, frag_size);
		}
		
		/// <summary>Concatenates smaller fragments into entire buffers.</summary>
		/// <param name="fragments">An array of byte buffers (<code>byte[]</code>)
		/// </param>
		/// <returns> A byte buffer
		/// </returns>
		public static byte[] defragmentBuffer(byte[][] fragments)
		{
			int total_length = 0;
			byte[] ret;
			int index = 0;
			
			if (fragments == null)
				return null;
			for (int i = 0; i < fragments.Length; i++)
			{
				if (fragments[i] == null)
					continue;
				total_length += fragments[i].Length;
			}
			ret = new byte[total_length];
			for (int i = 0; i < fragments.Length; i++)
			{
				if (fragments[i] == null)
					continue;
				Array.Copy(fragments[i], 0, ret, index, fragments[i].Length);
				index += fragments[i].Length;
			}
			return ret;
		}
		
		public static void  printFragments(byte[][] frags)
		{
			for (int i = 0; i < frags.Length; i++)
				System.Console.Out.WriteLine('\'' + new string(Global.ToCharArray(frags[i])) + '\'');
		}
		
		public static string shortName(string hostname)
		{
			int index;
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			
			if (hostname == null)
				return null;
			
			index = hostname.IndexOf((System.Char) '.');
			if (index > 0 && !System.Char.IsDigit(hostname[0]))
				sb.Append(hostname.Substring(0, (index) - (0)));
			else
				sb.Append(hostname);
			return sb.ToString();
		}
		
		/// <summary>Reads a number of characters from the current source Stream and writes the data to the target array at the specified index.</summary>
		/// <param name="sourceStream">The source Stream to read from.</param>
		/// <param name="target">Contains the array of characteres read from the source Stream.</param>
		/// <param name="start">The starting index of the target array.</param>
		/// <param name="count">The maximum number of characters to read from the source Stream.</param>
		/// <returns>The number of characters read. The number will be less than or equal to count depending on the data available in the source Stream. Returns -1 if the end of the stream is reached.</returns>
		public static System.Int32 ReadInput(System.Net.Sockets.Socket sock, byte[] target, int start, int count)
		{
			// Returns 0 bytes if not enough space in target
			if (target.Length == 0)
				return 0;

			int bytesRead,totalBytesRead = 0,buffStart = start; 
			
			while(true)
			{
                try
                {
                    if (!sock.Connected)
                        throw new ExtSocketException("socket closed");

                    bytesRead = sock.Receive(target, start, count, SocketFlags.None);

                    if (bytesRead == 0) throw new ExtSocketException("socket closed");

                    totalBytesRead += bytesRead;
                    if (bytesRead == count) break;
                    else
                        count = count - bytesRead;

                    start = start + bytesRead;
                }
                catch (SocketException e)
                {

                    if (e.SocketErrorCode == SocketError.NoBufferSpaceAvailable) continue;
				
					else throw;
                }
				
			}
	
			if (totalBytesRead == 0)	
				return -1;
                
                
			return totalBytesRead;
		}

        public static System.Int32 ReadInput(System.Net.Sockets.Socket sock, byte[] target, int start, int count,int min)
        {
            // Returns 0 bytes if not enough space in target
            if (target.Length == 0)
                return 0;

            int bytesRead, totalBytesRead = 0, buffStart = start;
            
            while (true)
            {
                try
                {
                    if (!sock.Connected)
                        throw new ExtSocketException("socket closed");

                    bytesRead = sock.Receive(target, start, count, SocketFlags.None);

                    if (bytesRead == 0) throw new ExtSocketException("socket closed");

                    totalBytesRead += bytesRead;
                    if (bytesRead == count) break;
                    else
                        count = count - bytesRead;

                    start = start + bytesRead;
                    if (totalBytesRead >min &&  sock.Available <= 0) break;
                }
                catch (SocketException e)
                {

                    if (e.SocketErrorCode == SocketError.NoBufferSpaceAvailable) continue;

                    else throw;
                }

            }

            // Returns -1 if EOF
            if (totalBytesRead == 0)
                return -1;


            return totalBytesRead;
        }
       
		/// <summary>
		/// Compares the two IP Addresses. Returns 0 if both are equal, 1 if ip1 is greateer than ip2 otherwise -1.
		/// </summary>
		/// <param name="ip1"></param>
		/// <param name="ip1"></param>
		/// <returns></returns>
		public static int CompareIP(IPAddress ip1,IPAddress ip2)
		{
			uint ipval1,ipval2;
			ipval1 = IPAddressToLong(ip1);
			ipval2 = IPAddressToLong(ip2);

			if(ipval1 == ipval2) return 0;
			if(ipval1 > ipval2) return 1;
			else return -1;
		}
		static private uint IPAddressToLong(IPAddress IPAddr)
		{			
			byte[] byteIP=IPAddr.GetAddressBytes();

			uint ip=(uint)byteIP[0]<<24;
			ip+=(uint)byteIP[1]<<16;
			ip+=(uint)byteIP[2]<<8;
			ip+=(uint)byteIP[3];

			return ip;
		}
		//:
		/// <summary>Reads a number of characters from the current source TextReader and writes the data to the target array at the specified index.</summary>
		/// <param name="sourceTextReader">The source TextReader to read from</param>
		/// <param name="target">Contains the array of characteres read from the source TextReader.</param>
		/// <param name="start">The starting index of the target array.</param>
		/// <param name="count">The maximum number of characters to read from the source TextReader.</param>
		/// <returns>The number of characters read. The number will be less than or equal to count depending on the data available in the source TextReader. Returns -1 if the end of the stream is reached.</returns>
		public static System.Int32 ReadInput(System.IO.TextReader sourceTextReader, byte[] target, int start, int count)
		{
			// Returns 0 bytes if not enough space in target
			if (target.Length == 0) return 0;

			char[] charArray = new char[target.Length];
			int bytesRead = sourceTextReader.Read(charArray, start, count);

			// Returns -1 if EOF
			if (bytesRead == 0) return -1;

			for(int index=start; index<start+bytesRead; index++)
				target[index] = (byte)charArray[index];

			return bytesRead;
		}
		/// <summary>
		/// Creates a byte buffer representation of a <c>int32</c>
		/// </summary>
		/// <param name="value"><c>int</c> to be converted</param>
		/// <returns>Byte Buffer representation of a <c>Int32</c></returns>
		public static byte[] WriteInt32(int value)
		{
			byte[] _byteBuffer = new byte[4];
			_byteBuffer[0] = (byte)value;
			_byteBuffer[1] = (byte)(value >> 8);
			_byteBuffer[2] = (byte)(value >> 16);
			_byteBuffer[3] = (byte)(value >> 24);
			
			return _byteBuffer;
		} 

        /// <summary>
        /// Creates a <c>Int32</c> from a byte buffer representation
        /// </summary>
        /// <param name="_byteBuffer">Byte Buffer representation of a <c>Int32</c></param>
        /// <returns></returns>
        public static int convertToInt32(byte[] _byteBuffer)
        {
            return (int)((_byteBuffer[0] & 0xFF) |
                _byteBuffer[1] << 8 |
                _byteBuffer[2] << 16 |
                _byteBuffer[3] << 24);
        } 

        /// <summary>
        /// Creates a <c>Int32</c> from a byte buffer representation
        /// </summary>
        /// <param name="_byteBuffer">Byte Buffer representation of a <c>Int32</c></param>
        /// <returns></returns>
        public static int convertToInt32(byte[] _byteBuffer, int offset)
        {
            byte[] temp = new byte[4];
            System.Buffer.BlockCopy(_byteBuffer, offset, temp, 0, 4);
            return convertToInt32(temp);
        } 

	}
}