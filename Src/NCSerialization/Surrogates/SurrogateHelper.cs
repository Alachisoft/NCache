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
using System.Net;
using System.Collections;
using System.Reflection;
//using System.Web.SessionState;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using System.Reflection.Emit;
using Alachisoft.NCache.Runtime.Serialization;
using System.Web;
using Alachisoft.NCache.Runtime.Serialization.IO;


namespace Alachisoft.NCache.Serialization.Surrogates
{
    public class SurrogateHelper
    {
        public static object CreateGenericType(string name, params Type[] types)
        {
            string t = name + "`" + types.Length;
            Type generic = Type.GetType(t).MakeGenericType(types);
            return Activator.CreateInstance(generic);
        }

        /// <summary>
        /// Instantiates an instance of a Generic type definition with the given set of type argumentss
        /// </summary>
        /// <param name="genericType">the generic type definition to instantiate an instance of</param>
        /// <param name="typeParams">the set of type parameters to use</param>
        /// <exception cref="ArgumentNullException">thrown when <paramref name="genericType"/> is null</exception>
        /// <exception cref="ArgumentException">thrown if <paramref name="genericType"/> is not a
        /// generic type definition</exception>
        /// <returns>a new instance of the generic type</returns>
        public static object CreateGenericTypeInstance(Type genericType, params Type[] typeParams)
        {
            if (genericType == null)
                throw new ArgumentNullException("genericType");
            //if (!genericType.IsGenericTypeDefinition)
            //    throw new ArgumentException(Resources.Type_NotGenericDef, "genericType");

            try
            {
                genericType = genericType.MakeGenericType(typeParams);
            }
            catch (InvalidOperationException op)
            {
                throw op;
            }
            catch (ArgumentNullException argN)
            {
                throw argN;
            }
            catch (ArgumentException arg)
            {
                throw arg;
            }
            catch (NotSupportedException nop)
            {
                throw nop;
            }

            return Activator.CreateInstance(genericType);
        }

        public static DefaultConstructorDelegate CreateDefaultConstructorDelegate(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("genericType");


            ConstructorInfo defaultConstructor = type.GetConstructor(Type.EmptyTypes);

            if (defaultConstructor == null) return null; 
            // Specify the types of the parameters of the dynamic method. This 
            // dynamic method has no parameters.
            Type[] methodArgs = Type.EmptyTypes;

            // Create a dynamic method with the name "", a return type
            // of void, and two parameters whose types are specified by
            // the array methodArgs. Create the method in the module that
            // defines the type.
            DynamicMethod method = new DynamicMethod(String.Empty,
                                MethodAttributes.Public | MethodAttributes.Static,
                                CallingConventions.Standard,
                                typeof(object),
                                methodArgs,
                                type,
                                true);

            // Get an ILGenerator and emit a body for the dynamic method,
            ILGenerator il = method.GetILGenerator();
            EmitDefaultCreatorMethod(type, il);

            DefaultConstructorDelegate deleg =
                (DefaultConstructorDelegate)method.CreateDelegate(typeof(DefaultConstructorDelegate));

            return deleg;
        }

        internal static void EmitDefaultCreatorMethod(Type type, ILGenerator il)
        {
            // Declare local variables for the method.
            LocalBuilder objLocal = il.DeclareLocal(typeof(object));

            if (!type.IsValueType)
            {
                ConstructorInfo constructor =
                    type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                              null,
                                              Type.EmptyTypes,
                                              null);
                if (constructor == null)
                {
                    throw new ArgumentException("constructor not found", type.Name);
                }
                il.Emit(OpCodes.Newobj, constructor);
            }
            else
            {
                LocalBuilder objRet = il.DeclareLocal(type);
                il.Emit(OpCodes.Ldloca_S, objRet);
                il.Emit(OpCodes.Initobj, type);
                il.Emit(OpCodes.Ldloc_1);
                il.Emit(OpCodes.Box, type);
            }

            // Assign to local variable objLocal
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0);

            // Return from the method.
            il.Emit(OpCodes.Ret);
        }
    }
}
