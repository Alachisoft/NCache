using System;
using System.Collections;

namespace Alachisoft.NGroups.Blocks
{
    internal class BinaryMessage
    {
        private IList buffer;
        private Array userPayLoad;
        private DateTime _time = DateTime.Now;

        public BinaryMessage(IList buf, Array userpayLoad)
        {
            buffer = buf;
            userPayLoad = userpayLoad;
        }

        public IList Buffer
        {
            get { return buffer; }
        }
        public Array UserPayLoad
        {
            get { return userPayLoad; }
        }

        public int Size
        {
            get
            {
                int size = 0;
                if (buffer != null)
                {
                    foreach(byte[] buff in buffer)
                        size += buff.Length;
                }
                if (userPayLoad != null)
                {
                    for (int i = 0; i < userPayLoad.Length; i++)
                    {
                        byte[] tmp = userPayLoad.GetValue(i) as byte[];
                        if (tmp != null) size += tmp.Length;
                    }
                }
                return size;
            }
        }

    }
}