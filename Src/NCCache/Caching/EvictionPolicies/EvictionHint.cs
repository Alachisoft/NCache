// Copyright (c) 2017 Alachisoft
// 
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
using System.Runtime.Serialization;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;


namespace Alachisoft.NCache.Caching.EvictionPolicies
{
    /// <summary>
	/// Abstract base class that serves as a placeholder for eviction specific 
	/// data on object basis. For example a priority based eviction policy needs 
	/// to set a priority with every object. Similarly other eviction may 
	/// need such kind of associations.
	/// </summary>    
    [Serializable]
	public abstract class EvictionHint : ICompactSerializable//: IComparable
	{

        /// <summary>
        /// Contain In Memory Size of Eviction Hint
        /// </summary>
        internal static int InMemorySize = Common.MemoryUtil.NetEnumSize; // for _hintType

		/// <summary>
		/// Get the slot in which this hint should be placed
		/// </summary>
		//public abstract byte SlotIndex {get;}

       
		[CLSCompliant(false)]
        public EvictionHintType _hintType;
        
        /// <summary>
		/// Return if hint is to be changed on Update
		/// </summary>
		public abstract bool IsVariant
		{
			get;
		}

		/// <summary>
		/// update the eviction value
		/// </summary>
		public abstract bool Update();


        public static EvictionHint ReadEvcHint(CompactReader reader)
        {
            EvictionHintType expHint = EvictionHintType.Parent;
            expHint = (EvictionHintType)reader.ReadInt16();
            EvictionHint tmpObj = null;
            switch (expHint)
            {
                case EvictionHintType.NULL:
                    return null;
                
                case EvictionHintType.Parent:
                    tmpObj = (EvictionHint)reader.ReadObject();
                    return (EvictionHint)tmpObj;

                case EvictionHintType.PriorityEvictionHint:
                    PriorityEvictionHint peh = new PriorityEvictionHint();
                    ((ICompactSerializable)peh).Deserialize(reader);
                    return (EvictionHint)peh;            
                
                default:
                    break;            
            }
            return null;
        }


        public static void WriteEvcHint(CompactWriter writer, EvictionHint evcHint)
        {
            if (evcHint == null)
            {
                writer.Write((short)EvictionHintType.NULL);
                return;                
            }

            writer.Write((short)evcHint._hintType);
            ((ICompactSerializable)evcHint).Serialize(writer);
            return;
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _hintType = (EvictionHintType)reader.ReadInt16();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write((short)_hintType);
        }

        #endregion
    }

}
