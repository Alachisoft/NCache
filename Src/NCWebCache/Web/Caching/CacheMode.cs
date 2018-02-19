// Copyright (c) 2018 Alachisoft
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


namespace Alachisoft.NCache.Web.Caching
{
	/// <summary>
	/// Specifies the startup mode (also known as isolation level) of <see cref="Cache"/>.
	/// </summary>
	/// <remarks>
	/// This enumeration allows you to control the the startup mode of <see cref="Cache"/> programmatically. The startup 
	/// mode <see cref="CacheMode.OutProc"/> corresponds to a High isolation level implying that 
	/// the <see cref="Cache"/> runs in NCache service's process. 
	/// Similarly <see cref="CacheMode.InProc"/> implies that <see cref="Cache"/> is inproc to the applications. 
	/// <see cref="CacheMode.InProc"/> is equal to specfying no mode at all, and in that case the mode
	/// specified in cache configuration is used.
	/// <para>
	/// An isolated cache can be shared between applications on the same node. Morever an isolated cache's lifetime 
	/// is explicitly controlled by using NCache Manager application.
	/// </para>
	/// </remarks>
	[Serializable]
	public enum CacheMode
	{
		/// <summary>
		/// Use the startup mode specified in the configuration.
		/// </summary>
		Default,

		/// <summary>
		/// Start the cache inproc, i.e., with a low isolation level.
		/// </summary>
		InProc,

		/// <summary>
		/// Start the cache outproc, i.e., with a high isolation level.
		/// </summary>
		OutProc
	}
}
