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
using System.Collections.Generic;

namespace Enyim.Caching.Configuration
{
	/// <summary>
	/// Defines an interface for configuring the authentication paramaters the <see cref="T:MemcachedClient"/>.
	/// </summary>
	public interface IAuthenticationConfiguration
	{
		/// <summary>
		/// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.IAuthenticationProvider"/> which will be used authehticate the connections to the Memcached nodes.
		/// </summary>
		Type Type { get; set; }

		/// <summary>
		/// Gets or sets the parameters passed to the authenticator instance.
		/// </summary>
		Dictionary<string, object> Parameters { get; }
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
