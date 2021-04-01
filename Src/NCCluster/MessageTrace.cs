using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NGroups
{
    internal class MessageTrace : ICompactSerializable
    {
        string _trace;
        HPTime _timeStamp;

        public MessageTrace(string trace)
        {
            _trace = trace;
            _timeStamp = HPTime.Now;
        }

        public override string ToString()
        {
            string toString = "";
            if (_trace != null)
            {
                toString = _trace + " : " + _timeStamp.ToString();
            }
            return toString;
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _trace = reader.ReadObject() as string;
            _timeStamp = reader.ReadObject() as HPTime;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_trace);
            writer.WriteObject(_timeStamp);
        }

        #endregion
    }
}