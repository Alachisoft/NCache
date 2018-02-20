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
using System;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NGroups.Util
{
	
	
	/// <summary> A bounded subclass of List, oldest elements are removed once max capacity is exceeded</summary>
	/// <author>  Bela Ban Nov 20, 2003
	/// </author>
	/// <version>  $Id: BoundedList.java,v 1.2 2004/07/26 15:23:26 belaban Exp $
	/// </version>
	[Serializable]
    internal class BoundedList : List, Runtime.Serialization.ICompactSerializable
	{
		internal int max_capacity = 10;
		
		
		
		public BoundedList()
		{
		}
		
		public BoundedList(int size):base()
		{
			max_capacity = size;
		}
		
		
		/// <summary> Adds an element at the tail. Removes an object from the head if capacity is exceeded</summary>
		/// <param name="obj">The object to be added
		/// </param>
		public override void  add(object obj)
		{
			if (obj == null)
				return ;
			while (_size >= max_capacity && _size > 0)
			{
				removeFromHead();
			}
			base.add(obj);
		}
		
		
		/// <summary> Adds an object to the head, removes an element from the tail if capacity has been exceeded</summary>
		/// <param name="obj">The object to be added
		/// </param>
		public override void  addAtHead(object obj)
		{
			if (obj == null)
				return ;
			while (_size >= max_capacity && _size > 0)
			{
				remove();
			}
			base.addAtHead(obj);
		}

        #region ICompact Serializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            max_capacity = reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(max_capacity);
            
        } 
        #endregion
    }
}