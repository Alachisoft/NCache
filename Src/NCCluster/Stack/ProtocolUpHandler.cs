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
using System;
using System.Threading;

namespace Alachisoft.NGroups.Stack
{
    internal class ProtocolUpHandler : ThreadClass
    {
        private Alachisoft.NCache.Common.DataStructures.Queue mq;
        private Protocol handler;
        int id;
        DateTime time;
        TimeSpan worsTime = new TimeSpan(0, 0, 0);


        public ProtocolUpHandler(Alachisoft.NCache.Common.DataStructures.Queue mq, Protocol handler)
        {
            this.mq = mq;
            this.handler = handler;
            if (handler != null)
            {
                Name = "UpHandler (" + handler.Name + ')';
            }
            else
            {
                Name = "UpHandler";
            }
            IsBackground = true;
        }

        public ProtocolUpHandler(Alachisoft.NCache.Common.DataStructures.Queue mq, Protocol handler, string name, int id)
        {
            this.mq = mq;
            this.handler = handler;
            if (name != null)
                Name = name;
            IsBackground = true;
            this.id = id;
        }


        /// <summary>Removes events from mq and calls handler.up(evt) </summary>
        override public void Run()
        {
            if (handler.Stack.NCacheLog.IsInfoEnabled) handler.Stack.NCacheLog.Info(Name, "---> Started!");
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

                        if (handler.enableMonitoring)
                        {
                            handler.PublishUpQueueStats(mq.Count, id);
                        }

                        time = DateTime.Now;
                        handler.up(evt);
                        DateTime now = DateTime.Now;
                        TimeSpan ts = now - time;

                        if (ts.TotalMilliseconds > worsTime.TotalMilliseconds)
                            worsTime = ts;
                    }
                    catch (QueueClosedException e)
                    {
                        handler.Stack.NCacheLog.Error(Name, e.ToString());
                        break;
                    }
                    catch (ThreadInterruptedException ex)
                    {
                        handler.Stack.NCacheLog.Error(Name, ex.ToString());
                        break;
                    }
                    catch (System.Exception e)
                    {
                        handler.Stack.NCacheLog.Error(Name, " exception: " + e.ToString());
                    }
                }
            }
            catch (ThreadInterruptedException ex)
            {
                handler.Stack.NCacheLog.Error(Name, ex.ToString());
            }
            if (handler.Stack.NCacheLog.IsInfoEnabled) handler.Stack.NCacheLog.Info(Name + "    ---> Stopped!");
        }
    }
}
