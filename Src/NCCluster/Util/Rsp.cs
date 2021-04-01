// $Id: Rsp.java,v 1.1.1.1 2003/09/09 01:24:12 belaban Exp $
using System;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System.Collections;

namespace Alachisoft.NGroups.Util
{
	/// <summary> class that represents a response from a communication</summary>
	public class Rsp
	{
		public object Value { get { return retval; } }
		public object Sender { get { return sender; } }

		/* flag that represents whether the response was received */
		internal bool received = false;
		/* flag that represents whether the response was suspected */
		internal bool suspected = false;
		/* The sender of this response */
		internal object sender = null;
		/* the value from the response */
		internal object retval = null;

        /* Time taken on the cluster for an operation*/
        internal long cluserTimetaken = 0;
        /* Time taken by the application to perform the operation*/
        internal long appTimetaken = 0;
		
		internal Rsp(object sender)
		{
			this.sender = sender;
		}
		
		internal Rsp(object sender, bool suspected)
		{
			this.sender = sender;
			this.suspected = suspected;
		}
		
		internal Rsp(object sender, object retval)
		{
			this.sender = sender;
			this.retval = retval;
			received = true;
		}

        internal Rsp(object sender, object retval,long clusterTime,long appTime)
        {
            this.sender = sender;
            this.retval = retval;
            this.cluserTimetaken = clusterTime;
            this.appTimetaken = appTime;
            received = true;
        }

		public void Deflate(string serializationContext)
		{
            if (retval != null)
            {
                if (retval is OperationResponse)
                    ((OperationResponse)retval).SerializablePayload = DeserailizeResponse(((OperationResponse)retval).SerializablePayload,serializationContext);
                else
                    retval = DeserailizeResponse(retval, serializationContext);
            }
		}


        public static object DeserailizeResponse(object response,string context)
        {
            object result = null;
            if (response is byte[])
                result = CompactBinaryFormatter.FromByteBuffer((byte[])response, context);
            else if (response is IList)
            {
                IList buffers = response as IList;
                ClusteredMemoryStream stream = new ClusteredMemoryStream(0);
                foreach (byte[] buffer in buffers)
                {
                    stream.Write(buffer, 0, buffer.Length);
                }
                stream.Position = 0;
                result = CompactBinaryFormatter.Deserialize(stream, context);
            }

            return result;
        }
		public bool wasReceived()
		{
			return received;
		}
		
		public bool wasSuspected()
		{
			return suspected;
		}
		

		public override string ToString()
		{
			return "sender=" + sender + ", retval=" + retval + ", received=" + received + ", suspected=" + suspected;
		}


        public long ClusterTimeTaken
        {
            get { return cluserTimetaken; }
            set { cluserTimetaken = value; }
        }

        public long AppTimeTaken
        {
            get { return appTimetaken; }
            set { appTimetaken = value; }
        }
	}
}