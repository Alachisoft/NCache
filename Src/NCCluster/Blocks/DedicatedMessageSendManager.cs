using System;
using System.Collections;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NGroups.Blocks
{
    internal class DedicatedMessageSendManager : IDisposable
    {
        private Alachisoft.NCache.Common.DataStructures.Queue mq;
        private object sync_lock = new object();
        private ArrayList senderList = new ArrayList();

        ILogger _ncacheLog;



        public DedicatedMessageSendManager(ILogger NCacheLog)
        {
            mq = new Alachisoft.NCache.Common.DataStructures.Queue();

            _ncacheLog = NCacheLog;
        }

        public void AddDedicatedSenderThread(ConnectionTable.Connection connection, bool onPrimaryNIC)
        {

            DedicatedMessageSender dmSender = new DedicatedMessageSender(mq, connection, sync_lock, _ncacheLog, onPrimaryNIC);

            dmSender.Start();
            lock (senderList.SyncRoot)
            {
                senderList.Add(dmSender);
            }

        }

        public void UpdateConnection(ConnectionTable.Connection newCon)
        {
            lock (senderList.SyncRoot)
            {
                foreach (DedicatedMessageSender dmSender in senderList)
                {
                    dmSender.UpdateConnection(newCon);
                }
            }
        }

        public int QueueMessage(IList buffer, Array userPayLoad, Priority prt)
        {
            return QueueMessage(new BinaryMessage(buffer, userPayLoad),prt);
        }

        public int QueueMessage(BinaryMessage bmsg,Priority prt)
        {
            int queueCount = 0;
            if (mq != null)
            {
                try
                {
                    mq.add(bmsg,prt);
                    queueCount = mq.Count;
                }
                catch (Exception e)
                {

                }
            }
            return queueCount;
        }
        #region IDisposable Members

        public void Dispose()
        {
            
            lock (senderList.SyncRoot)
            {
                foreach (DedicatedMessageSender dmSender in senderList)
                {
                    if (dmSender.IsAlive)
                    {
                        try
                        {
                            _ncacheLog.Flush();
#if !NETCORE
                            dmSender.Abort();
#elif NETCORE
                            dmSender.Interrupt();
#endif
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
                senderList.Clear();
                mq.close(false);
            }
        }

        #endregion
    }
}