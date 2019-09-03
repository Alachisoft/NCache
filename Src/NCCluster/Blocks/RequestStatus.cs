using System;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NGroups.Blocks
{
    [Serializable]
    public class RequestStatus : ICompactSerializable
    {
        public const byte REQ_NOT_RECEIVED = 1;
        public const byte REQ_RECEIVED_NOT_PROCESSED = 2;
        public const byte REQ_PROCESSED = 4;
        public const byte NONE = 8;
        
        public const int CLEAN_TIME = 15; //Seconds.

        long _reqId;
        byte _status = REQ_NOT_RECEIVED;

        DateTime? _creationTime;

        
        public RequestStatus(long reqId) 
        {
            _reqId = reqId;
            _creationTime = DateTime.Now;
        }
        public RequestStatus(long reqId,byte status):this(reqId)
        {
            _status = status;
        }
        /// <summary>
        /// Gets the request id.
        /// </summary>
        public long ReqId { get { return _reqId; } }

        /// <summary>
        /// Gets the status of the request. Following can be the status
        /// RequestStatus.REQ_NOT_RECEIVED -> request not received at this node
        /// RequestStatus.REQ_RECEIVED_NOT_PROCESSED -> request received but not yet processed.
        /// RequestStatus.REQ_PROCESSED -> request received,processed and response sent.
        /// </summary>
        public byte Status
        {
            get { return _status; }
        }

        /// <summary>
        /// Marks the request as arrived.
        /// </summary>
        public void MarkReceived()
        {
            _status = REQ_RECEIVED_NOT_PROCESSED;
        }
        /// <summary>
        /// Marks the request as processd.
        /// </summary>
        public void MarkProcessed()
        {
            _status = REQ_PROCESSED;
        }

        public bool HasExpired()
        {

            if (_creationTime != null)
            {
                TimeSpan? time_passed = DateTime.Now - _creationTime;
                if (time_passed.Value.TotalSeconds >= CLEAN_TIME)
                    return true;
                else
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("RequestStatus[req_id="+ _reqId + ";status=");

            switch (_status)
            {
                case REQ_NOT_RECEIVED:
                    sb.Append("REQ_NOT_RECEIVED");
                    break;
                case REQ_PROCESSED:
                    sb.Append("REQ_PROCESSED");
                    break;
                case REQ_RECEIVED_NOT_PROCESSED:
                    sb.Append("REQ_RECEIVED_NOT_PROCESSED");
                    break;
                case NONE:
                    sb.Append("NONE");
                    break;
            }
            return sb.ToString();
        }

        #region ICompactSerializable Members
        public void Deserialize(CompactReader reader)
        {
            _reqId = reader.ReadInt32();
            _status = reader.ReadByte();
            _creationTime = (DateTime?)reader.ReadObject();

        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(_reqId); 
            writer.Write(_status);
            writer.WriteObject(_creationTime); // you cannot write nullable datetime with simple write so using writeObject
      
        } 
        #endregion
    }
}