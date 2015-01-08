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



using Alachisoft.NCache.Runtime.Serialization.IO;


using Alachisoft.NCache.Runtime.Serialization;



namespace Alachisoft.NGroups.Util
{
	/// <summary>
	/// Represents a range of messages that need retransmission. Contains the first and last seqeunce numbers.
	/// <p><b>Author:</b> Chris Koiak, Bela Ban</p>
	/// <p><b>Date:</b>  12/03/2003</p>
	/// </summary>
	[Serializable]
	internal class Range : ICompactSerializable
	{
		public long low = - 1; // first msg to be retransmitted
		public long high = - 1; // last msg to be retransmitted
		
		/// <summary>For externalization </summary>
		public Range()
		{
		}
		
		public Range(long low, long high)
		{
			this.low = low; this.high = high;
		}
		
		public override string ToString()
		{
			return "[" + low + " : " + high + ']';
		}

		#region ICompactSerializable Members

		void ICompactSerializable.Deserialize(CompactReader reader)
		{
			low = reader.ReadInt64();
			high = reader.ReadInt64();
		}

		void ICompactSerializable.Serialize(CompactWriter writer)
		{
			writer.Write(low);
			writer.Write(high);
		}

		#endregion
	}
}
