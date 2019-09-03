//  Copyright (c) 2018 Alachisoft
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


/// <summary>
/// The namespace provides public interface <see cref="ICompactSerializable"/>.
/// Any type that you want the compact serialization framework to serialize/deserialize and 
/// not the .NET framework, must implement this interface.
/// </summary>

namespace Alachisoft.NCache.Runtime.Serialization
{
	/// <summary> 
	/// Implementations of ICompactSerializable can add their state directly to the output stream, 
	/// enabling them to bypass costly serialization.
	/// </summary>
	/// <remarks>
    /// Objects that implement <see cref="ICompactSerializable"/> must have a default 
	/// constructor (can be private). 
	/// <para>
	/// As per current implementation when a <see cref="ICompactSerializable"/> is deserialized 
	/// the default constructor is not invoked, therefore the object must "construct" itself in 
	/// <see cref="ICompactSerializable.Deserialize"/>.
	/// </para>
	/// </remarks>
	[CLSCompliant(false)]
	public interface ICompactSerializable
	{
		/// <summary>
		/// Load the state from the passed stream reader object.
		/// </summary>
		/// <param name="reader">A <see cref="CompactBinaryReader"/> object</param>
		/// <remarks>
		/// As per current implementation when a <see cref="ICompactSerializable"/> is deserialized 
		/// the default constructor is not invoked, therefore the object must "construct" itself in 
		/// <see cref="ICompactSerializable.Deserialize"/>.
		/// </remarks>
		[CLSCompliant(false)]
		void Deserialize(CompactReader reader);

		/// <summary>
		/// Save the state to the passed stream reader object.
		/// </summary>
		/// <param name="writer">A <see cref="BinaryWriter"/> object</param>
		[CLSCompliant(false)]
		void Serialize(CompactWriter writer);
	}
}
