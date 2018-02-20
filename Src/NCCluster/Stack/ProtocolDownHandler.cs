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
// $Id: Protocol.java,v 1.18 2004/07/05 14:17:33 belaban Exp $
using Alachisoft.NGroups.Util;
using System.Threading;

namespace Alachisoft.NGroups.Stack
{
    internal class ProtocolDownHandler : ThreadClass
    {
        private Alachisoft.NCache.Common.DataStructures.Queue mq;
        private Protocol handler;
        int id;

        public ProtocolDownHandler(Alachisoft.NCache.Common.DataStructures.Queue mq, Protocol handler)
        {
            this.mq = mq;
            this.handler = handler;
            string name = null;
            if (handler != null)
            {
                Name = "DownHandler (" + handler.Name + ')';
            }
            else
            {
                Name = "DownHandler";
            }

            IsBackground = true;
        }
        public ProtocolDownHandler(Alachisoft.NCache.Common.DataStructures.Queue mq, Protocol handler, string name, int id)
        {
            this.mq = mq;
            this.handler = handler;
            Name = name;
            IsBackground = true;
            this.id = id;
        }


        /// <summary>Removes events from mq and calls handler.down(evt) </summary>
        override public void Run()
        {
            try
            {
                while (!mq.Closed)
                {
                    try
                    {
                        Event evt = (Event)mq.remove();
                        if (evt == null)
                        {
                            handler.Stack.NCacheLog.Warn("Protocol", "removed null event");
                            continue;
                        }

                        int type = evt.Type;
                        if (type == Event.ACK || type == Event.START || type == Event.STOP)
                        {
                            if (handler.handleSpecialDownEvent(evt) == false)
                                continue;
                        }

                        if (handler.enableMonitoring)
                        {
                            handler.PublishDownQueueStats(mq.Count, id);
                        }

                        handler.down(evt);
                    }
                    catch (QueueClosedException e)
                    {
                        handler.Stack.NCacheLog.Error(Name, e.ToString());
                        break;
                    }
                    catch (ThreadInterruptedException e)
                    {
                        handler.Stack.NCacheLog.Error(Name, e.ToString());
                        break;
                    }
                    catch (System.Exception e)
                    {
                        handler.Stack.NCacheLog.Warn(Name, " exception is " + e.ToString());
                    }
                }
            }
            catch (ThreadInterruptedException e)
            {
                handler.Stack.NCacheLog.Error("DownHandler.Run():3", "exception=" + e.ToString());
            }
        }
    }
}
