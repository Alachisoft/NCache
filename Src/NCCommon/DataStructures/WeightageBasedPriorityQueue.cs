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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Common.DataStructures
{
    public class WeightageBasedPriorityQueue : Queue
    {
        private IQueueDistributionStrategy _distributionStrategy;

        public WeightageBasedPriorityQueue(int lowPriorityMessagePercentage):base()
        {
            _distributionStrategy = new WeightageBasedQueueDistribution(lowPriorityMessagePercentage);
        }

        public WeightageBasedPriorityQueue():this(30)
        {
        }

        protected override object RemoveItemFromQueue()
        {          
            Object retval = null;

            //we always check for critical pritority messages first
            if (_queues[(int)Priority.High].Count > 0)
                retval = removeInternal(_queues[(int)Priority.High]);

            if (retval == null)
            {
                Priority desiredPriority = _distributionStrategy.GetDistributionPriority();

                switch (desiredPriority)
                {
                    case Priority.Normal:
                        if (_queues[(int)Priority.Normal].Count > 0)
                            retval = removeInternal(_queues[(int)Priority.Normal]);
                        break;

                    case Priority.Low:
                        if (_queues[(int)Priority.Low].Count > 0)
                            retval = removeInternal(_queues[(int)Priority.Low]);
                        break;
                }
            }

            if (retval == null)
            {
                //there is no item with the desired priority, therefore we will run the default alogorithm of choosing items from the queue
                retval = base.RemoveItemFromQueue();
            }

            return retval;
        }
    }
}
