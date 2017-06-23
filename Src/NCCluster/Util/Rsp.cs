// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// $Id: Rsp.java,v 1.1.1.1 2003/09/09 01:24:12 belaban Exp $

using System;
using Alachisoft.NCache.Serialization.Formatters;

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
                    ((OperationResponse)retval).SerializablePayload = CompactBinaryFormatter.FromByteBuffer((byte[]) ((OperationResponse)retval).SerializablePayload, serializationContext);
                else if (retval is Byte[])
                    retval = CompactBinaryFormatter.FromByteBuffer((byte[])retval, serializationContext);
            }
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
