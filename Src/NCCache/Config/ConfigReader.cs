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
using System.Data;
using System.Collections;

namespace Alachisoft.NCache.Config
{
	/// <summary>
	/// Base configuration reader.
	/// </summary>
	public abstract class ConfigReader
	{
		abstract public Hashtable Properties
		{
			get;
		}

		/// <summary>
		/// Returns an xml config given a properties map.
		/// </summary>
		/// <param name="properties">properties map</param>
		/// <returns>xml config.</returns>
		static public string ToPropertiesXml(IDictionary properties)
		{
			return ToPropertiesXml(properties, false);
		}

		/// <summary>
		/// Returns an xml config given a properties map.
		/// </summary>
		/// <param name="properties">properties map</param>
		/// <returns>xml config.</returns>
		static public string ToPropertiesXml(IDictionary properties, bool formatted)
		{
			return ConfigHelper.CreatePropertiesXml(properties, 0, formatted);
		}

        static public string ToPropertiesXml2(IDictionary properties, bool formatted)
        {
            return ConfigHelper.CreatePropertiesXml2(properties, 0, formatted);
        }

		/// <summary>
		/// Returns a property string given a properties map.
		/// </summary>
		/// <param name="properties">properties map</param>
		/// <returns>property string</returns>
		static public string ToPropertiesString(IDictionary properties)
		{
			return ToPropertiesString(properties, false);
		}

		/// <summary>
		/// Returns a property string given a properties map.
		/// </summary>
		/// <param name="properties">properties map</param>
		/// <returns>property string</returns>
		static public string ToPropertiesString(IDictionary properties, bool formatted)
		{
			return ConfigHelper.CreatePropertyString(properties, 0, formatted);
		}
	}
}
