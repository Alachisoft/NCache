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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Linq.Expressions;

namespace Enyim.Reflection
{
	/// <summary>
	/// <para>Implements a very fast object factory for dynamic object creation. Dynamically generates a factory class which will use the new() constructor of the requested type.</para>
	/// <para>Much faster than using Activator at the price of the first invocation being significantly slower than subsequent calls.</para>
	/// </summary>
	public static class FastActivator
	{
		private static Dictionary<Type, Func<object>> factoryCache = new Dictionary<Type, Func<object>>();

		/// <summary>
		/// Creates an instance of the specified type using a generated factory to avoid using Reflection.
		/// </summary>
		/// <typeparam name="T">The type to be created.</typeparam>
		/// <returns>The newly created instance.</returns>
		public static T Create<T>()
		{
			return TypeFactory<T>.Create();
		}

		/// <summary>
		/// Creates an instance of the specified type using a generated factory to avoid using Reflection.
		/// </summary>
		/// <param name="type">The type to be created.</param>
		/// <returns>The newly created instance.</returns>
		public static object Create(Type type)
		{
			Func<object> f;

			if (!factoryCache.TryGetValue(type, out f))
				lock (factoryCache)
					if (!factoryCache.TryGetValue(type, out f))
					{
						factoryCache[type] = f = Expression.Lambda<Func<object>>(Expression.New(type)).Compile();
					}

			return f();
		}

		private static class TypeFactory<T>
		{
			public static readonly Func<T> Create = Expression.Lambda<Func<T>>(Expression.New(typeof(T))).Compile();
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
