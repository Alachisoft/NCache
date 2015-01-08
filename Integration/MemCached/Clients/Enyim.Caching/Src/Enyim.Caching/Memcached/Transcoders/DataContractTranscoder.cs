// Copyright (c) 2015 Alachisoft
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
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Default <see cref="T:Enyim.Caching.Memcached.ITranscoder"/> implementation. Primitive types are manually serialized, the rest is serialized using <see cref="T:System.Runtime.Serialization.NetDataContractSerializer"/>.
	/// </summary>
	public class DataContractTranscoder : DefaultTranscoder
	{
		protected override object DeserializeObject(ArraySegment<byte> value)
		{
			var ds = new NetDataContractSerializer();

			using (var ms = new MemoryStream(value.Array, value.Offset, value.Count))
				return ds.Deserialize(ms);
		}

		protected override ArraySegment<byte> SerializeObject(object value)
		{
			using (var ms = new MemoryStream())
			{
				new NetDataContractSerializer().Serialize(ms, value);

				return new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Length);
			}
		}
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kisk�, enyim.com
 *    
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *    
 *        http://www.apache.org/licenses/LICENSE-2.0
 *    
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *    
 * ************************************************************/
#endregion
