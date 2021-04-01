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
using System.Reflection;
#if !NETCORE
using System.Runtime.Remoting;
#endif

namespace Alachisoft.NCache.Common
{
    /// <summary>
    /// class to assist with common system and .NET operations.
    /// </summary>
    public class ReflectionUtil
    {
        /// <summary>
        /// Creates an instance of the type whose name is specified, using the named 
        /// assembly and the default constructor.
        /// </summary>
        /// <param name="assembly">The name of the assembly where the type named typeName 
        /// is sought. If assemblyName is a null reference, 
        /// the executing assembly is searched. </param>
        /// <param name="classname">The name of the preferred type. </param>
        /// <returns>A reference to the newly created instance.</returns>
        public static object CreateObject(string assembly, string classname)
        {
            return CreateObject(assembly, classname, null);
        }

        /// <summary>
        /// Creates an instance of the type whose name is specified, using the named 
        /// assembly and the constructor that best matches the specified parameters.
        /// </summary>
        /// <param name="assembly">The name of the assembly where the type named typeName 
        /// is sought. If assemblyName is a null reference, 
        /// the executing assembly is searched. </param>
        /// <param name="classname">The name of the preferred type. </param>
        /// <param name="args">An array of arguments that match in number, order, and 
        /// type the parameters of the constructor to invoke.</param>
        /// <returns>A reference to the newly created instance.</returns>
        public static object CreateObject(string assembly, string classname, object[] args)
        {
            try
            {
#if !NETCORE
                ObjectHandle objHandle = null;
                objHandle = Activator.CreateInstance(
                    assembly,				// name of assembly
                    classname,				// fully qualified class name
                    false,					// class name is case-sensitive
                    BindingFlags.Default,	// no binding attributes	
                    null,					// use default binder
                    args,					// arguments to constructor,
                    null,					// default culture
                    null,					// default activation attributes
                    null					// default security policy
                    );
                if (objHandle != null)
                {
                    return objHandle.Unwrap();
                }
#elif NETCORE
                var obj = Activator.CreateInstance(
                    assembly.GetType(),             // type of assembly
                    classname,				// fully qualified class name
                    false,					// class name is case-sensitive
                    BindingFlags.Default,	// no binding attributes	
                    null,					// use default binder
                    args,					// arguments to constructor,
                    null,					// default culture
                    null,					// default activation attributes
                    null					// default security policy
                    );
                return obj;
#endif

            }
            catch (Exception e)
            {
                Trace.error("Common.CreateObject()", e.ToString());
                throw;
            }
            return null;
        }
    }
}