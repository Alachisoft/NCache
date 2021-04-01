using System;
using System.Collections;
using System.IO;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Stats;
using System.Text;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NGroups.Blocks;
using Alachisoft.NGroups.Protocols;

namespace Alachisoft.NGroups
{
    /// <summary>
    /// Enumeration that defines the quality-of-service for messages.
    /// </summary>
    //	[Flags]
    //	public enum Qos
    //	{
    //		None			= 0x0000,
    //		Reliable		= 0x0001,
    //		Ordered			= 0x0002,
    //		Default			= Ordered | Reliable
    //	}
    //
    /// <remarks>
    /// A Message encapsulates data sent to members of a group. 
    /// It contains among other things the address of the sender, 
    /// the destination address, a payload (byte buffer) and a list of 
    /// headers. Headers are added by protocols on the sender side and 
    /// removed by protocols on the receiver's side.
    /// </remarks>
    /// <summary>
    /// Message passed between members of a group.
    /// <p><b>Author:</b> Chris Koiak, Bela Ban</p>
    /// <p><b>Date:</b>  12/03/2003</p>
    /// </summary>
    [Serializable]
	public class Message : ICompactSerializable, IRentableObject, ICustomSerializable
	{
		/// <summary>Destination of the message</summary>
		protected Address		dest_addr;

		/// <summary>A list containing addresses of the recepient nodes of an mcast message.</summary>
		protected ArrayList		dest_addrs;

		/// <summary>The flag that if true then TOTAL ensures sequencing of the message. Otherwise TOTAL just bypass it.</summary>
		private bool			_isSeqRequired;

		/// <summary>Source of the message</summary>
		protected Address		src_addr;
		/// <summary> Prioirty of this message during transit. </summary>
		protected Priority		prio = Priority.Normal;
		/// <summary>Headers added to the message</summary>
		private Hashtable		headers = null;
		/// <summary>Byte buffer of payload associated with the message</summary>
		protected byte[]		buf = null;
		/// <summary> Flag that separates transactional traffic from management one. </summary>
		private bool			isUserMsg;

        public bool             responseExpected = false;

        [NonSerialized]
			[CLSCompliant(false)]
        public HPTimeStats _timeToSendReq;
        [NonSerialized]
		[CLSCompliant(false)]
		public HPTimeStats _loopTime;

		[CLSCompliant(false)]
		public HPTimeStats _timeToTakeSeq;
		[CLSCompliant(false)]
		public HPTimeStats _TotalToTcpdownStats;
		[CLSCompliant(false)]
		public HPTimeStats _TotalWaitStats;

        private bool isProfilable;

        private long profileid;
        /// <summary>Stack stats</summary>
        private long psTime;
        /// <summary>transport layer stats</summary>
        private long tnspTime;

        private int rentId;
        private string traceMsg = String.Empty;

        private long reqId;
        private bool handledAsynchronously;
		[CLSCompliant(false)]
		public byte _type = MsgType.SEQUENCE_LESS;

		/// <summary>The index into the payload (usually 0) </summary>
		[NonSerialized]
		private int				offset = 0;
		/// <summary>The number of bytes in the buffer (usually buf.length is buf != null) </summary>
		[NonSerialized]
		private int				length = 0;
		internal const long ADDRESS_OVERHEAD = 400; // estimated size of Address (src and dest)

        private Array _payload;
		[CLSCompliant(false)]
		public Hashtable _stackTrace;
		[CLSCompliant(false)]
		public Hashtable _stackTrace2;
        DateTime sendTime;
        DateTime arrivalTime;

        private Stream stream = null;
        private IList buffers;


		/// <summary>Only used for Externalization (creating an initial object) </summary>
		public  Message()
		{
			headers = new Hashtable();
			isUserMsg = false;
        } // should not be called as normal constructor

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="dest">Destination of the message</param>
		/// <param name="src">Source of the message</param>
		/// <param name="buf">Byte buffer of payload associated with the message</param>
		public Message(Address dest, Address src, byte[] buf) 
		{
			dest_addr = dest;
			src_addr = src;
			setBuffer(buf);
			headers = new Hashtable();
			isUserMsg = false;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dest">Destination of the message</param>
        /// <param name="src">Source of the message</param>
        /// <param name="buf">Byte buffer of payload associated with the message</param>
        public Message(Address dest, Address src, IList buffers)
        {
            dest_addr = dest;
            src_addr = src;
            Buffers = buffers;
            headers = new Hashtable();
            isUserMsg = false;
        }

		/// <summary> Constructs a message. The index and length parameters allow to provide a <em>reference</em> to
		/// a byte buffer, rather than a copy, and refer to a subset of the buffer. This is important when
		/// we want to avoid copying. When the message is serialized, only the subset is serialized.
		/// </summary>
		/// <param name="dest">Address of receiver. If it is <em>null</em> or a <em>string</em>, then
		/// it is sent to the group (either to current group or to the group as given
		/// in the string). If it is a Vector, then it contains a number of addresses
		/// to which it must be sent. Otherwise, it contains a single destination.<p>
		/// Addresses are generally untyped (all are of type <em>Object</em>. A channel
		/// instance must know what types of addresses it expects and downcast
		/// accordingly.
		/// </param>
		/// <param name="src">   Address of sender
		/// </param>
		/// <param name="buf">   A reference to a byte buffer
		/// </param>
		/// <param name="offset">The index into the byte buffer
		/// </param>
		/// <param name="length">The number of bytes to be used from <tt>buf</tt>. Both index and length are checked for
		/// array index violations and an ArrayIndexOutOfBoundsException will be thrown if invalid
		/// </param>
		public Message(Address dest, Address src, byte[] buf, int offset, int length)
		{
			dest_addr = dest;
			src_addr = src;
			headers = new Hashtable();
			isUserMsg = false;
			setBuffer(buf, offset, length);
        }

        /// <summary>
        /// Gets or sets the trace message.
        /// </summary>
        public string TraceMsg
        {
            get { return traceMsg; }
            set { traceMsg = value; }
        }

        public byte Type
        {
            get { return _type; }
            set { _type = value; }
        }
        /// <summary>
        /// Gets or set the id of the request.
        /// </summary>
        public long RequestId
        {
            get { return reqId; }
            set { reqId = value; }
        }

        /// <summary>
        /// Indicates wheter message should be processed asynchronously or not.
        /// </summary>
        public bool HandledAysnc
        {
            get { return handledAsynchronously; }
            set { handledAsynchronously = value; }
        }

        /// <summary>
        /// Gets or set the Stream for reuse
        /// </summary>
        public Stream SerlizationStream
        {
            get { return stream; }
            set
            {
                if (value != null)
                {
                    stream = value;
                    this.length = (int)stream.Length;
                }
            }
        }


        public void MarkSent()
        {
            sendTime = DateTime.Now;
        }

        public void MarkArrived()
        {
            arrivalTime = DateTime.Now;
        }
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="dest">Destination of the message</param>
		/// <param name="src">Source of the message</param>
		/// <param name="obj">Serialisable payload OR array of <c>Message</c>s</param>
		public Message(Address dest, Address src, Object obj) 
		{
			dest_addr = dest;
			src_addr = src;
			headers = new Hashtable();
			if(obj!=null)
			{
				if(obj.GetType().IsSerializable)
					setObject(obj);
				else
					throw new Exception("Message can only contain an Array of messages or an ISerializable object");
			}
		}

		public Address Dest
		{
			get{return dest_addr;}
			set{dest_addr = value;}
		}

		public ArrayList Dests
		{
			get { return dest_addrs; }
			set { dest_addrs = value; }
		}

		public Address Src
		{
			get{return src_addr;}
			set{src_addr = value;}
		}

		public bool IsSeqRequired
		{
			get { return _isSeqRequired; }
			set { _isSeqRequired = value; }
		}

		/// <summary>
		/// Gets and sets the payload (bute buffer) of the message
		/// </summary>
		public byte[] RawBuffer
		{
			get{return buf;}
			set{buf = value;}
		}

		/// <summary>Returns the offset into the buffer at which the data starts </summary>
		public int Offset
		{
			get{return offset;}			
		}

		/// <summary>Returns the number of bytes in the buffer </summary>
		public int Length
		{
			get{return length;}
			set { length = value; }
		}

        /// <summary>Returns the number of bytes in the buffer </summary>
        public int BufferLength
        {
            get 
            {
                if (Buffers != null)
                {
                    int blength = 0;
                    foreach (byte[] buffer in buffers)
                    {
                        blength += buffer.Length; 
                    }
                    return blength;
                }
                return 0;
            }
        }		

		/// <summary>
		/// The number of backup caches configured with this instance.
		/// </summary>
		public Priority Priority
		{
			get { return prio; }
			set { prio = value;	}
		}

		/// <summary> 
		/// Flag that separates transactional traffic from management one. 
		/// </summary>
		public bool IsUserMsg
		{
			get { return isUserMsg; }
			set { isUserMsg = value; }
		}

        /// <summary>
        /// Indicates whether the message is profilable or not.
        /// Flag used for performance benchmarking.
        /// </summary>
        public bool IsProfilable
        {
            get { return isProfilable; }
            set { isProfilable = value; }
        }

        /// <summary>
        /// Gets or sets the profile id.
        /// </summary>
        public long ProfileId
        {
            get { return profileid; }
            set { profileid = value; }
        }

        public long StackTimeTaken
        {
            get { return psTime; }
            set { psTime = value; }
        }

        public long TransportTimeTaken
        {
            get { return tnspTime; }
            set { tnspTime = value; }
        }

		/// <summary>
		/// Gets the collection of Headers added to the message
		/// </summary>
		public Hashtable Headers
		{
			get{return headers;}
			set { headers = value; }
		}

		/// <summary>
		/// Compares a second <c>Message</c> for equality
		/// </summary>
		/// <param name="obj">Second Message object</param>
		/// <returns>True if Messages are equal</returns>
		public override bool Equals(Object obj)
		{
			if (!(obj is Message))
				return false;
			Message msg2 = (Message)obj;
			if ((dest_addr == null || msg2.Dest == null) && 
				(msg2.Dest!=null || dest_addr != null))
				return false;
			else if(dest_addr != null && !dest_addr.Equals(msg2.Dest))
				return false;
			
			if ((src_addr == null || msg2.Src == null) && 
				(msg2.Src!=null || src_addr != null))
				return false;
			else if(src_addr != null && !src_addr.Equals(msg2.Src))
				return false;

			if (buf != msg2.RawBuffer ||buffers != msg2.buffers||
				headers.Count != msg2.Headers.Count)
				return false;

			foreach(DictionaryEntry h in headers)
			{
				if(!msg2.Headers.Contains(h.Key))
					return false;
			}

			return true;
		}

        public void AddTrace(Address node, string trace)
        {
            if (_stackTrace == null) _stackTrace = new Hashtable();
            lock (_stackTrace.SyncRoot)
            {
                ArrayList traceList = null;
                if (_stackTrace.Contains(node))
                {
                    traceList = _stackTrace[node] as ArrayList;
                }
                else
                {
                    traceList = new ArrayList();
                    _stackTrace.Add(node, traceList);
                }
                traceList.Add(new MessageTrace(trace));
            }
        }
        public string GetTrace()
        {
            return GetTraceInternal(_stackTrace);
        }
        public string GetTrace2()
        {
            return GetTraceInternal(_stackTrace2);
        }
        private string GetTraceInternal(Hashtable traceTable)
        {
            string trace = "";
            StringBuilder sb = null;
            if (traceTable != null)
            {
                sb = new StringBuilder();
                lock (traceTable.SyncRoot)
                {
                    IDictionaryEnumerator ide = traceTable.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        Address node = ide.Key as Address;
                        ArrayList traceList = ide.Value as ArrayList;
                        if (traceList != null)
                        {
                            sb.Append("TraceNode : " + node + " [ ");
                            for (int i = 0; i < traceList.Count; i++)
                            {
                                sb.Append(traceList[i].ToString() + " ; ");
                            }
                            sb.Append(" ] ");
                        }
                    }
                }
                trace = sb.ToString();
            }
            return trace;
        }
		/// <summary>
		/// Returns the hash value for a Message
		/// </summary>
		/// <returns>The hash value for a Message</returns>
		public override int GetHashCode()
		{
			int retValue = headers == null ? 0:headers.GetHashCode();
			ArrayList hc = new ArrayList();
			if (dest_addr!=null)
				hc.Add(dest_addr.GetHashCode());
			if (src_addr!=null)
				hc.Add(src_addr.GetHashCode());
            if (buf != null)
                hc.Add(buf.GetHashCode());
            if (buffers!=null)
				hc.Add(buffers.GetHashCode());

			for(int i=0;i<hc.Count;i++)
				retValue = retValue.GetHashCode() ^ hc[i].GetHashCode();

			return retValue;
		}

		/// <summary> Returns a copy of the buffer if offset and length are used, otherwise a reference</summary>
		/// <returns>
		/// </returns>
		public virtual byte[] getBuffer()
		{
			if (buf == null)
				return null;
			if (offset == 0 && length == buf.Length)
				return buf;
			else
			{
				byte[] retval = new byte[length];
				Array.Copy(buf, offset, retval, 0, length);
				return retval;
			}
		}
		
		public virtual void  setBuffer(byte[] b)
		{
			buf = b;
			if (buf != null)
			{
				offset = 0;
				length = buf.Length;
			}
			else
			{
				offset = length = 0;
			}
		}

        public IList Buffers
        {
            get { return buffers; }
            set { buffers = value; }
        }

		/// <summary> Set the internal buffer to point to a subset of a given buffer</summary>
		/// <param name="b">The reference to a given buffer. If null, we'll reset the buffer to null
		/// </param>
		/// <param name="offset">The initial position
		/// </param>
		/// <param name="length">The number of bytes
		/// </param>
		public virtual void  setBuffer(byte[] b, int offset, int length)
		{
			buf = b;
			if (buf != null)
			{
				if (offset < 0 || offset > buf.Length)
					throw new System. IndexOutOfRangeException("Index of bound " + offset);
				if ((offset + length) > buf.Length)
					throw new System. IndexOutOfRangeException("Index of bound " + (offset + length));
				this.offset = offset;
				this.length = length;
			}
			else
			{
				offset = length = 0;
			}
		}

		/// <summary>
		/// Serialises an object in to the payload
		/// </summary>
		/// <param name="obj">Object to serialise</param>
		public void setObject(Object obj) 
		{
			if (buf != null)
				return;
			if(!obj.GetType().IsSerializable)
				throw new Exception("Specified object for message is not serializable");
			setBuffer(CompactBinaryFormatter.ToByteBuffer(obj,null));
		}

		/// <summary>
		/// Deserialises an Object from the payload
		/// </summary>
		/// <returns>Deserialised Object</returns>
		public Object getObject() 
		{
			if (buf == null)
				return null;
			return CompactBinaryFormatter.FromByteBuffer(buf,null);
		}

		/// <summary>
		/// Get the pay load without deserializing it.
		/// </summary>
		/// <returns>Deserialised Object</returns>
		public Object getFlatObject() 
		{
			return buf;
		}


		/// <summary> Nulls all fields of this message so that the message can be reused. Removes all headers from the
		/// stack, but keeps the stack
		/// </summary>
		public virtual void  reset()
		{
			dest_addr = src_addr = null;
			setBuffer(null);
			if (headers != null)
				headers.Clear();
			prio = Priority.Normal;
            this.isProfilable = false;
            profileid = psTime = tnspTime =0;
            traceMsg = String.Empty;
			isUserMsg = false;
		}

		/// <summary>
		/// Creates a byte buffer representation of a <c>long</c>
		/// </summary>
		/// <param name="value"><c>long</c> to be converted</param>
		/// <returns>Byte Buffer representation of a <c>long</c></returns>
		public byte[] WriteInt64(long value)
		{
			byte[] _byteBuffer = new byte[8];
			_byteBuffer[0] = (byte)value;
			_byteBuffer[1] = (byte)(value >> 8);
			_byteBuffer[2] = (byte)(value >> 16);
			_byteBuffer[3] = (byte)(value >> 24);
			_byteBuffer[4] = (byte)(value >> 32);
			_byteBuffer[5] = (byte)(value >> 40);
			_byteBuffer[6] = (byte)(value >> 48);
			_byteBuffer[7] = (byte)(value >> 56);
			return _byteBuffer;
		} // WriteInt32

		/// <summary>
		/// Creates a <c>long</c> from a byte buffer representation
		/// </summary>
		/// <param name="_byteBuffer">Byte Buffer representation of a <c>long</c></param>
		/// <returns></returns>
		private long convertToLong(byte[] _byteBuffer) 
		{        
			return (long)((_byteBuffer[0] & 0xFF) |
				_byteBuffer[1] << 8 |
				_byteBuffer[2] << 16 |
				_byteBuffer[3] << 24 |
				_byteBuffer[4] << 32 |
				_byteBuffer[5] << 40 |
				_byteBuffer[6] << 48 |
				_byteBuffer[7] << 56);
		} // ReadInt32


		
		public virtual Message copy()
		{
			return copy(true,null);
		}
        public virtual Message copy(MemoryManager memManager)
        {
            return copy(true, memManager);
        }
		/// <summary> Create a copy of the message. If offset and length are used (to refer to another buffer), the copy will
		/// contain only the subset offset and length point to, copying the subset into the new copy.
		/// </summary>
		/// <param name="">copy_buffer
		/// </param>
		/// <returns>
		/// </returns>
		public virtual Message copy(bool copy_buffer,MemoryManager memManaager)
		{
            Message retval = null;
            if (memManaager != null)
            {
                ObjectProvider provider = memManaager.GetProvider(typeof(Message));
                if(provider != null)
                    retval = (Message)provider.RentAnObject();
            }
            else
                retval = new Message();

			retval.dest_addr = dest_addr;
			retval.dest_addrs = dest_addrs;
			retval.src_addr = src_addr;
			retval.prio = prio;
			retval.isUserMsg = isUserMsg;
            retval.isProfilable = IsProfilable;
            retval.ProfileId = profileid;
            retval.psTime = psTime;
            retval.tnspTime = tnspTime;
            retval.traceMsg = traceMsg;
            retval.RequestId = reqId;
            retval.handledAsynchronously = handledAsynchronously;
            retval.responseExpected = responseExpected;

			if (copy_buffer && buf != null)
			{
				retval.setBuffer(buf, offset, length);
			}
            if (copy_buffer && buffers != null)
            {
                retval.buffers = buffers;
            }
            if (headers != null)
            {
                retval.headers = (Hashtable)headers.Clone();
            }
            retval.Payload = this.Payload;
			return retval;
		}

       
		public virtual object Clone()
		{
			return copy();
		}

		public virtual Message makeReply(ObjectProvider MsgProvider)
		{
            Message m = null;
            if (MsgProvider != null)
            {
                m = (Message) MsgProvider.RentAnObject();
            }
            else
                m = new Message(src_addr, null, null);

            m.dest_addr = src_addr;
            m.profileid = profileid;
            m.isProfilable = isProfilable;
            return m;
		}
        public virtual Message makeReply( )
        {
            Message m =  new Message(src_addr, null, null);
			m.IsUserMsg = IsUserMsg;
            m.dest_addr = src_addr;
            m.profileid = profileid;
            m.isProfilable = isProfilable;
            if(IsProfilable)  m.TraceMsg = traceMsg + "-->complete";
            m.Priority = prio;
            return m;
        }
		/// <summary> Returns size of buffer, plus some constant overhead for src and dest, plus number of headers time
		/// some estimated size/header. The latter is needed because we don't want to marshal all headers just
		/// to find out their size requirements. If a header implements Sizeable, the we can get the correct
		/// size.<p> Size estimations don't have to be very accurate since this is mainly used by FRAG to
		/// determine whether to fragment a message or not. Fragmentation will then serialize the message,
		/// therefore getting the correct value.
		/// </summary>
		public virtual long size()
		{
			long retval = length;
			long hdr_size = 0;
			Header hdr;
			
			if (dest_addr != null)
				retval += ADDRESS_OVERHEAD;
			if (src_addr != null)
				retval += ADDRESS_OVERHEAD;
			
			if (headers != null)
			{
				for (IEnumerator it = headers.Values.GetEnumerator(); it.MoveNext(); )
				{
					hdr = (Header) it.Current;
					if (hdr == null)
						continue;
					hdr_size = hdr.size();
					if (hdr_size <= 0)
						hdr_size = Header.HDR_OVERHEAD;
					else
						retval += hdr_size;
				}
			}
			return retval;
		}

		/*---------------------- Used by protocol layers ----------------------*/
		/// <summary>
		/// Gets a header associated with a Protocol layer
		/// </summary>
		/// <param name="key">Protocol Name associated with the header</param>
		/// <returns>Implementation of the HDR class</returns>
		public Header getHeader(object key) 
		{
			if(headers != null && headers.Contains(key))
				return (Header)headers[key];
			return null;
		}

		/// <summary>
		/// Adds a header in to the Message
		/// </summary>
		/// <param name="key">Protocol Name associated with the header</param>
		/// <param name="hdr">Implementation of the HDR class</param>
		public void putHeader(object key, Header hdr) 
		{
			try
			{
				headers[key] = hdr;
			}
			catch(ArgumentException e)
			{
			}
		}

		/// <summary>
		/// Removes a header associated with a Protocol layer
		/// </summary>
		/// <param name="key">Protocol Name associated with the header</param>
		/// <returns>Implementation of the HDR class</returns>
		public Header removeHeader(object key) 
		{
			Header retValue = (Header)headers[key];
			headers.Remove(key);
			return retValue;
		}

		/// <summary>
		/// Clears all Headers from message
		/// </summary>
		public void removeHeaders() 
		{
			if(headers != null)
				headers.Clear();
		}

		public virtual string printObjectHeaders()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			DictionaryEntry entry;
			
			if (headers != null)
			{
				for (IEnumerator it = headers.GetEnumerator(); it.MoveNext(); )
				{
					entry = (DictionaryEntry) it.Current;
					sb.Append(entry.Key).Append(": ").Append(entry.Value).Append('\n');
				}
			}
			return sb.ToString();
		}

        public Array Payload
        {
            get { return _payload; }
            set { _payload = value; }
        }

		#region ICompactSerializable Members

		void ICompactSerializable.Deserialize(CompactReader reader)
		{
			dest_addr = (Address) reader.ReadObject();
			dest_addrs = (ArrayList)reader.ReadObject();
            src_addr = (Address)reader.ReadObject();
			prio = (Priority) Enum.ToObject(typeof(Priority), reader.ReadInt16());
            Boolean isStream = reader.ReadBoolean();
            if (isStream)
            {
                int len = reader.ReadInt32();
                buf = reader.ReadBytes(len);
            }
            else
            {
                buf = (byte[])reader.ReadObject();
            }

            int bufferCount = reader.ReadInt32();

            if (bufferCount > 0)
            {
                buffers = new ClusteredArrayList(bufferCount);

                for (int i = 0; i < bufferCount; i++)
                {
                    buffers.Add(reader.ReadBytes(reader.ReadInt32()));
                }
            }

            
            headers = (Hashtable)reader.ReadObject();
            handledAsynchronously = reader.ReadBoolean();
            responseExpected = reader.ReadBoolean();
            _type = reader.ReadByte();
            _stackTrace = reader.ReadObject() as Hashtable;

			offset = 0;
			length = (buf != null) ? buf.Length : 0;
		}

		void ICompactSerializable.Serialize(CompactWriter writer)
		{
			writer.WriteObject(dest_addr);
			writer.WriteObject(dest_addrs);
            writer.WriteObject(src_addr);
			writer.Write(Convert.ToInt16(prio));
            if (stream != null)
            {
                writer.Write(true);
                writer.Write(length);
                writer.Write(((MemoryStream)stream).GetBuffer(), 0, length);
            }
            else
            {
                writer.Write(false);
                writer.WriteObject((object)buf);
            }

            if (Buffers != null)
            {
                writer.Write(buffers.Count);
                foreach (byte[] buff in Buffers)
                {
                    writer.Write(buff.Length);
                    writer.Write(buff, 0, buff.Length);
                }
            }
            else
                writer.Write((int)0);

            writer.WriteObject(headers);
            writer.Write(handledAsynchronously);
            writer.Write(responseExpected);
            writer.Write(_type);
            writer.WriteObject(_stackTrace);

		}

		#endregion

        #region IRentableObject Members

        public int RentId
        {
            get
            {
                return rentId;
            }
            set
            {
                rentId = value;
            }
        }

        #endregion

		#region ICustomSerializable Members

		public void DeserializeLocal(BinaryReader reader)
		{
			isUserMsg = true;
			reader.BaseStream.Position -= 1;
			byte flags = reader.ReadByte();
			FlagsByte bFlags = new FlagsByte();
			bFlags.DataByte = flags;
			//Headers are in sequence 1. COR  2. TOTAL 3. TCP
			headers = new Hashtable();
			if (bFlags.AnyOn(FlagsByte.Flag.COR))
			{
				RequestCorrelator.HDR corHdr = new RequestCorrelator.HDR();
				corHdr.DeserializeLocal(reader);
				headers.Add(HeaderType.REQUEST_COORELATOR, corHdr);
			}

			if (bFlags.AnyOn(FlagsByte.Flag.TOTAL))
			{
				TOTAL.HDR totalHdr = new TOTAL.HDR();
				totalHdr.DeserializeLocal(reader);
				headers.Add(HeaderType.TOTAL, totalHdr);

			}

			if (bFlags.AnyOn(FlagsByte.Flag.TCP))
			{
				TcpHeader tcpHdr = new TcpHeader();
				tcpHdr.DeserializeLocal(reader);
				headers.Add(HeaderType.TCP, tcpHdr);
			}

			prio = (Priority)Enum.ToObject(typeof(Priority), reader.ReadInt16());
			handledAsynchronously = reader.ReadBoolean();
            long ticks = reader.ReadInt64();
            arrivalTime = new DateTime(ticks);
            ticks = reader.ReadInt64();
            sendTime = new DateTime(ticks);
            responseExpected = reader.ReadBoolean();
            _type = reader.ReadByte();
          
            bool bufferNotNull = reader.ReadBoolean();
           
                length = reader.ReadInt32();
            if(bufferNotNull)
                buf = (byte[])reader.ReadBytes(length);

            bool bufferListNotNull = reader.ReadBoolean();
                int bufferCount = reader.ReadInt32();
                
                if (bufferListNotNull)
                {
                    buffers = new ClusteredArrayList(bufferCount);

                    for (int i = 0; i < bufferCount; i++)
                    {
                        buffers.Add(reader.ReadBytes(reader.ReadInt32()));
                    }
                }
                           
		}

        public void SerializeLocal(BinaryWriter writer)
		{
			//Check in sequence of headers.. 
			FlagsByte bFlags = new FlagsByte();
			if (IsUserMsg)
				bFlags.SetOn(FlagsByte.Flag.TRANS);

			object tmpHdr;
			tmpHdr = (Header)headers[(object)(HeaderType.REQUEST_COORELATOR)];
			if (tmpHdr != null)
			{
				RequestCorrelator.HDR corHdr = (RequestCorrelator.HDR)tmpHdr;
				corHdr.SerializeLocal(writer);
				bFlags.SetOn(FlagsByte.Flag.COR);
			}

			tmpHdr = (Header)headers[(object)(HeaderType.TOTAL)];
			if (tmpHdr != null)
			{
				TOTAL.HDR totalHdr = (TOTAL.HDR)tmpHdr;
				totalHdr.SerializeLocal(writer);
				bFlags.SetOn(FlagsByte.Flag.TOTAL);
			}

			tmpHdr = (Header)headers[(object)(HeaderType.TCP)];
			if (tmpHdr != null)
			{
				TcpHeader tcpHdr = (TcpHeader)tmpHdr;
				tcpHdr.SerializeLocal(writer);
				bFlags.SetOn(FlagsByte.Flag.TCP);
			}

			writer.Write(Convert.ToInt16(prio));
			writer.Write(handledAsynchronously);
            writer.Write(arrivalTime.Ticks);
            writer.Write(sendTime.Ticks);
            writer.Write(responseExpected);
            writer.Write(_type);
            if (stream != null)
            {
                writer.Write(true);
                writer.Write(length);
                writer.Write(((MemoryStream)stream).GetBuffer(), 0, length);
            }
            else
            {
                writer.Write(buf != null);
                int length =  buf != null ? buf.Length: 0;
                writer.Write(length);
                if(buf != null) writer.Write(buf);
                
            }

            writer.Write(Buffers != null);
            int bufferCount = Buffers != null ? Buffers.Count : 0;
            writer.Write(bufferCount);

            if (Buffers != null)
            {
                foreach (byte[] buff in Buffers)
                {
                    writer.Write(buff.Length);
                    writer.Write(buff, 0, buff.Length);
                }
            }
           
			long curPos = writer.BaseStream.Position;
			writer.BaseStream.Position = 8; //afte 4 bytes of total size and 4 bytes of message size ..here comes the flag.
			writer.Write(bFlags.DataByte);
			writer.BaseStream.Position = curPos;
		}

		#endregion
    }
}

