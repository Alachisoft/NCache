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
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.Pooling.Lease;
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.Caching.EvictionPolicies
{
    /// <summary>
	/// Abstract base class that serves as a placeholder for eviction specific 
	/// data on object basis. For example a priority based eviction policy needs 
	/// to set a priority with every object. Similarly other eviction may 
	/// need such kind of associations.
	/// </summary>    
    [Serializable]
	public abstract class EvictionHint : SimpleLease, ICompactSerializable
	{

        ///// <summary>
        ///// Compares the current instance with another object of the same type.
        ///// </summary>
        ///// <param name="obj">An object to compare with this instance.</param>
        ///// <returns>A 32-bit signed integer that indicates the relative order of the comparands.</returns>
		[CLSCompliant(false)]
        public EvictionHintType _hintType;


        internal static int InMemorySize = Common.MemoryUtil.NetEnumSize; // for _hintType

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

        public abstract void Reset(CacheItemPriority cacheItemPriority);

        //#endregion

        public static EvictionHint ReadEvcHint(CompactReader reader, PoolManager poolManager)
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
                    return tmpObj;

                case EvictionHintType.CounterHint:
                    var ch = CounterHint.Create(poolManager);
                    ((ICompactSerializable)ch).Deserialize(reader);
                    return ch;

                case EvictionHintType.PriorityEvictionHint:
                    var peh = PriorityEvictionHint.Create(poolManager);
                    ((ICompactSerializable)peh).Deserialize(reader);
                    return peh;

                case EvictionHintType.TimestampHint:
                    var tsh = TimestampHint.Create(poolManager);
                    ((ICompactSerializable)tsh).Deserialize(reader);
                    return tsh;               
                
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

        #region ILeasable

        public override void ResetLeasable()
        {
            _hintType = default(EvictionHintType);
        }

        public override void ReturnLeasableToPool()
        {
      
        }

        #endregion

        #region - [Deep Cloning] -

        public abstract EvictionHint DeepClone(PoolManager poolManager);

        #endregion
    }
}
