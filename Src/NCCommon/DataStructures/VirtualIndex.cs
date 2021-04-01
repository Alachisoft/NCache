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

namespace Alachisoft.NCache.Common.DataStructures
{
    public class VirtualIndex :IComparable
    {
        int maxSize = 79 * 1024;
        int x, y;

        public VirtualIndex() { }

        public VirtualIndex(int index)
        {
            IncrementBy(index);
        }
        
        public VirtualIndex(int maxSize,int index)
        {
            this.maxSize = maxSize;
            IncrementBy(index);
        }
        
        public void Increment()
        {
            x++;
            if (x == maxSize)
            {
                x = 0;
                y++;
            }
        }

        public void IncrementBy(int count)
        {
            int number = (this.y * maxSize) + this.x + count;
            this.x = number % maxSize;
            this.y = number / maxSize;
        }


        internal int XIndex
        {
            get { return x; }
        }

        internal int YIndex
        {
            get { return y; }
        }

        public int IndexValue
        {
            get { return (y * maxSize) + x; }
        }
        public VirtualIndex Clone()
        {
            VirtualIndex clone = new VirtualIndex();
            clone.x = x;
            clone.y = y;
            return clone;
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            VirtualIndex other = null;
            if (obj is VirtualIndex)
                other = obj as VirtualIndex;
            else if (obj is int)
                other = new VirtualIndex((int)obj);
            else
                return -1;

            if (other.IndexValue == IndexValue)
                return 0;
            else if (other.IndexValue > IndexValue)
                return -1;
            else
                return 1;
        }

        #endregion
    }
}