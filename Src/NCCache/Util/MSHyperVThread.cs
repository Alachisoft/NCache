//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
#if !NETCORE
using System.Management;
#endif
using System.Threading;

namespace Alachisoft.NCache.Util
{
    class MSHyperVThread
    {
        bool bIsHyperV = false;
        object lockObj = new Object();
        public int IsHyperV()
        {
            lock (lockObj)
            {
                Thread hyperThr = new Thread(new ThreadStart(this.CheckMSHyperV));
                hyperThr.Start();
                bool result = Monitor.Wait(lockObj, 1000);
#if !NETCORE
                hyperThr.Abort();
#elif NETCORE
                hyperThr.Interrupt();
#endif
            }
            return Convert.ToInt32(bIsHyperV);
        }


        public void CheckMSHyperV()
        {
#if !NETCORE
            try
            {  
            ObjectQuery MyobjectQuery = null;
                //query for "Win32_Processor" class under WMI
                MyobjectQuery = new ObjectQuery("select * from Win32_BaseBoard");

                //searcher is what runs the query, set the Query object as a parameter        
                ManagementObjectSearcher MySearcher = new ManagementObjectSearcher(MyobjectQuery);
                foreach (ManagementObject Mgmt in MySearcher.Get())
                {
                    foreach (ManagementObject SubMgmt in MySearcher.Get())
                    {
                        string manuf = (SubMgmt["Manufacturer"].ToString());
                        manuf = manuf.ToLower();
                        if (manuf.IndexOf("microsoft") != -1)
                        {
                            bIsHyperV = true;
                        }
                    }
                }

                lock (lockObj)
                {
                    Monitor.Pulse(lockObj);
                }
            }
            catch (Exception)
            {
                bIsHyperV = false;
            }
#elif NETCORE

            //TODO: ALACHISOFT(System.Management has some issues)
            throw new NotImplementedException();
#endif
        }
    }
}