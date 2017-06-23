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
using System.ComponentModel;
using System.Configuration;
using System.Collections.Generic;
using Enyim.Caching.Memcached;
using Enyim.Reflection;
using System.Xml.Linq;

namespace Enyim.Caching.Configuration
{
	/// <summary>
	/// This element is used to define locator/transcoder/keyTransformer instances. It also provides custom initializations for them using a factory.
	/// </summary>
	/// <typeparam name="TFactory"></typeparam>
	public class FactoryElement<TFactory> : ConfigurationElement
		where TFactory : class, IProvider
	{
		protected readonly Dictionary<string, string> Parameters = new Dictionary<string, string>();
		private TFactory instance;

		protected virtual bool IsOptional { get { return false; } }

		/// <summary>
		/// Gets or sets the type of the factory.
		/// </summary>
		[ConfigurationProperty("factory"), TypeConverter(typeof(TypeNameConverter))]
		public Type Factory
		{
			get { return (Type)base["factory"]; }
			set { base["factory"] = value; }
		}

		protected override bool OnDeserializeUnrecognizedAttribute(string name, string value)
		{
			ConfigurationProperty property = new ConfigurationProperty(name, typeof(string), value);
			base[property] = value;

			this.Parameters[name] = value;

			return true;
		}

		/// <summary>
		/// Creates the provider by using the factory (if present) or directly instantiating by type name
		/// </summary>
		/// <returns></returns>
		public TFactory CreateInstance()
		{
			//check if we have a factory
			if (this.instance == null)
			{
				var type = this.Factory;
				if (type == null)
				{
					if (this.IsOptional || !this.ElementInformation.IsPresent)
						return null;

					throw new ConfigurationErrorsException("factory must be defined");
				}

				this.instance = (TFactory)FastActivator.Create(type);
				this.instance.Initialize(this.Parameters);
			}

			return this.instance;
		}
	}

	public class OptionalFactoryElement<TResult> : FactoryElement<TResult>
		where TResult : class, IProvider
	{
		protected override bool IsOptional
		{
			get { return true; }
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
