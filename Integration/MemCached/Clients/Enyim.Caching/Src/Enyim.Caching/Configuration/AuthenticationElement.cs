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
using System.ComponentModel;
using System.Configuration;
using System.Collections.Generic;

namespace Enyim.Caching.Configuration
{
	/// <summary>
	/// Configures the authentication settings for Memcached servers.
	/// </summary>
	public sealed class AuthenticationElement : ConfigurationElement, IAuthenticationConfiguration
	{
		// TODO make this element play nice with the configuration system (allow saving, etc.)
		private Dictionary<string, object> parameters = new Dictionary<string, object>();

		/// <summary>
		/// Gets or sets the type of the <see cref="T:Enyim.Caching.Memcached.IAuthenticationProvider"/> which will be used authehticate the connections to the Memcached nodes.
		/// </summary>
		[ConfigurationProperty("type", IsRequired = false), TypeConverter(typeof(TypeNameConverter)), InterfaceValidator(typeof(Enyim.Caching.Memcached.ISaslAuthenticationProvider))]
		public Type Type
		{
			get { return (Type)base["type"]; }
			set { base["type"] = value; }
		}

		protected override bool OnDeserializeUnrecognizedAttribute(string name, string value)
		{
			var property = new ConfigurationProperty(name, typeof(string), value);
			base[property] = value;

			this.parameters[name] = value;

			return true;
		}

		#region [ IAuthenticationConfiguration ]

		Type IAuthenticationConfiguration.Type
		{
			get { return this.Type; }
			set
			{
				ConfigurationHelper.CheckForInterface(value, typeof(Enyim.Caching.Memcached.ISaslAuthenticationProvider));

				this.Type = value;
			}
		}

		System.Collections.Generic.Dictionary<string, object> IAuthenticationConfiguration.Parameters
		{
			// HACK we should return a clone, but i'm lazy now
			get { return this.parameters; }
		}

		#endregion
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
