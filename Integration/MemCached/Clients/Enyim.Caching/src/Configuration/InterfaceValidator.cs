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
using System.Configuration;

namespace Enyim.Caching.Configuration
{
	public class InterfaceValidator : ConfigurationValidatorBase
	{
		private Type interfaceType;

		public InterfaceValidator(Type type)
		{
			if (!type.IsInterface)
				throw new ArgumentException(type + " must be an interface");

			this.interfaceType = type;
		}

		public override bool CanValidate(Type type)
		{
			return (type == typeof(Type)) || base.CanValidate(type);
		}

		public override void Validate(object value)
		{
			if (value != null)
				ConfigurationHelper.CheckForInterface((Type)value, this.interfaceType);
		}
	}

	public sealed class InterfaceValidatorAttribute : ConfigurationValidatorAttribute
	{
		private Type interfaceType;

		public InterfaceValidatorAttribute(Type type)
		{
			if (!type.IsInterface)
				throw new ArgumentException(type + " must be an interface");

			this.interfaceType = type;
		}

		public override ConfigurationValidatorBase ValidatorInstance
		{
			get { return new InterfaceValidator(this.interfaceType); }
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
