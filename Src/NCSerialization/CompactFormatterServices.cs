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
using System.Collections;
using Alachisoft.NCache.Serialization.Surrogates;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Runtime.Serialization;


namespace Alachisoft.NCache.Serialization
{
    /// <summary>
    /// Provides methods to register <see cref="ICompactSerializable"/> implementations
    /// utilizing available surrogates.
    /// </summary>
    public sealed class CompactFormatterServices
    {
        static object mutex = new object();
        #region /       ICompactSerializable specific        /

        /// <summary>
        /// Registers a type that implements <see cref="ICompactSerializable"/> with the system. If the
        /// type is an array of <see cref="ICompactSerializable"/>s appropriate surrogates for arrays
        /// and the element type are also registered.
        /// </summary>
        /// <param name="type">type that implements <see cref="ICompactSerializable"/></param>
        /// <exception cref="ArgumentNullException">If <param name="type"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the <param name="type"/> is already registered or when no appropriate surrogate 
        /// is found for the specified <param name="type"/>.
        /// </exception>
        static public void RegisterCompactType(Type type, short typeHandle, IObjectPool pool = null)
        {
            //registers type as version compatible compact type
            RegisterCompactType(type, typeHandle, true, pool);
        }

        /// <summary>
        /// Registers a type that implements <see cref="ICompactSerializable"/> with the system. If the
        /// type is an array of <see cref="ICompactSerializable"/>s appropriate surrogates for arrays
        /// and the element type are also registered.
        /// </summary>
        /// <param name="type">type that implements <see cref="ICompactSerializable"/></param>
        /// <exception cref="ArgumentNullException">If <param name="type"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the <param name="type"/> is already registered or when no appropriate surrogate 
        /// is found for the specified <param name="type"/>.
        /// </exception>
        static public void RegisterNonVersionCompatibleCompactType(Type type, short typeHandle, IObjectPool pool = null)
        {
            RegisterCompactType(type, typeHandle, false, pool);
        }

        static private void RegisterCompactType(Type type, short typeHandle, bool versionCompatible, IObjectPool pool = null)
        {
            if (type == null) throw new ArgumentNullException("type");
            ISerializationSurrogate surrogate = null;

            if ((surrogate = TypeSurrogateSelector.GetSurrogateForTypeStrict(type,null)) != null)
            {
                if (surrogate.ObjectPool == null && pool != null)
                    surrogate.ObjectPool = pool;

                //No need to check subHandle since this funciton us not used by DataSharing
                if (surrogate.TypeHandle == typeHandle)
                    return; //Type is already registered with same handle.

                throw new ArgumentException("Type " + type.FullName + " is already registered with different handle");
            }

            //if (typeof(IDictionary).IsAssignableFrom(type))
            //{
            //    if (type.IsGenericType)
            //        surrogate = new GenericIDictionarySerializationSurrogate(typeof(IDictionary<,>));
            //    else
            //        surrogate = new IDictionarySerializationSurrogate(type);
            //}
            if (typeof(Dictionary<,>).Equals(type))
            {
                if (type.IsGenericType)
                    surrogate = new GenericIDictionarySerializationSurrogate(typeof(IDictionary<,>));
                else
                    surrogate = new IDictionarySerializationSurrogate(type);
            }
            else if (type.IsArray)            
            {
                surrogate = new ArraySerializationSurrogate(type);
            }
            //else if (typeof(IList).IsAssignableFrom(type))
            //{
            //    if (type.IsGenericType)
            //        surrogate = new GenericIListSerializationSurrogate(typeof(IList<>));
            //    else
            //        surrogate = new IListSerializationSurrogate(type);
            //}
            else if (typeof(List<>).Equals(type))
            {
                if (type.IsGenericType)
                    surrogate = new GenericIListSerializationSurrogate(typeof(IList<>));
                else
                    surrogate = new IListSerializationSurrogate(type);
            }
            else if (typeof(ICompactSerializable).IsAssignableFrom(type))
            {
                if (versionCompatible)
                    surrogate = new VersionCompatibleCompactSerializationSurrogate(type, pool);
                else
                    surrogate = new ICompactSerializableSerializationSurrogate(type, pool);
            }
            else if (typeof(Enum).IsAssignableFrom(type))
            {
                surrogate = new EnumSerializationSurrogate(type);
            }

            if (surrogate == null)
                throw new ArgumentException("No appropriate surrogate found for type " + type.FullName);

            TypeSurrogateSelector.RegisterTypeSurrogate(surrogate, typeHandle);
        }
		/// <summary>
		/// Registers a type that implements <see cref="ICompactSerializable"/> with the system. If the
		/// type is an array of <see cref="ICompactSerializable"/>s appropriate surrogates for arrays
		/// and the element type are also registered.
		/// </summary>
		/// <param name="type">type that implements <see cref="ICompactSerializable"/></param>
		/// <exception cref="ArgumentNullException">If <param name="type"/> is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// If the <param name="type"/> is already registered or when no appropriate surrogate 
		/// is found for the specified <param name="type"/>.
		/// </exception>
		static public void RegisterCustomCompactType(Type type, short typeHandle,string cacheContext, short subTypeHandle, Hashtable attributeOrder,bool portable,Hashtable nonCompactFields, IObjectPool pool = null)
		{
			if (type == null) throw new ArgumentNullException("type");
			ISerializationSurrogate surrogate = null; 

			if(cacheContext == null) throw new ArgumentException("cacheContext can not be null");

			if ((surrogate = TypeSurrogateSelector.GetSurrogateForTypeStrict(type,cacheContext)) != null)
			{
                if (surrogate.ObjectPool == null && pool != null)
                    surrogate.ObjectPool = pool;

                if (surrogate.TypeHandle == typeHandle && ( surrogate.SubTypeHandle == subTypeHandle || surrogate.SubTypeHandle != 0))
					return; //Type is already registered with same handle.

				throw new ArgumentException("Type " + type.FullName + " is already registered with different handle");
			}

            if (typeof(Dictionary<,>).Equals(type) && string.IsNullOrEmpty(((Type[])type.GetGenericArguments())[0].FullName))
            {
                if (type.IsGenericType)
                    surrogate = new GenericIDictionarySerializationSurrogate(typeof(IDictionary<,>));
                else
                    surrogate = new IDictionarySerializationSurrogate(type);
            }
            else if (type.IsArray)
           
			{
				surrogate = new ArraySerializationSurrogate(type);
			}
           
            else if (typeof(List<>).Equals(type) && string.IsNullOrEmpty(((Type[])type.GetGenericArguments())[0].FullName))
            {
                if (type.IsGenericType)
                    surrogate = new GenericIListSerializationSurrogate(typeof(IList<>));
                else
                    surrogate = new IListSerializationSurrogate(type);
            } 
			else if (typeof(ICompactSerializable).IsAssignableFrom(type))
			{
				surrogate = new ICompactSerializableSerializationSurrogate(type, pool);
			}
            else if (typeof(Enum).IsAssignableFrom(type))
            {
                surrogate = new EnumSerializationSurrogate(type);
            }
            else
            {

                lock (mutex)
                {
                DynamicSurrogateBuilder.Portable = portable;
                if (portable)
                    DynamicSurrogateBuilder.SubTypeHandle = subTypeHandle;
                surrogate = DynamicSurrogateBuilder.CreateTypeSurrogate(type, attributeOrder,nonCompactFields);
                }
            }

			if (surrogate == null)
				throw new ArgumentException("No appropriate surrogate found for type " + type.FullName);

			TypeSurrogateSelector.RegisterTypeSurrogate(surrogate, typeHandle,cacheContext, subTypeHandle, portable);
		}

        /// <summary>
        /// Registers a type that implements <see cref="ICompactSerializable"/> with the system. If the
        /// type is an array of <see cref="ICompactSerializable"/>s appropriate surrogates for arrays
        /// and the element type are also registered.
        /// </summary>
        /// <param name="type">type that implements <see cref="ICompactSerializable"/></param>
        /// <exception cref="ArgumentNullException">If <param name="type"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the <param name="type"/> is already registered or when no appropriate surrogate 
        /// is found for the specified <param name="type"/>.
        /// </exception>
        static public void RegisterCompactType(Type type, IObjectPool pool = null)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (TypeSurrogateSelector.GetSurrogateForTypeStrict(type,null) != null)
                throw new ArgumentException("Type '" + type.FullName + "' is already registered");

            ISerializationSurrogate surrogate = null;
            //if (typeof(IDictionary).IsAssignableFrom(type))
            //{
            //    surrogate = new IDictionarySerializationSurrogate(type);
            //}
            if (typeof(Dictionary<,>).Equals(type))
            {
                surrogate = new IDictionarySerializationSurrogate(type);
            }
            else if (type.IsArray)
            {
                surrogate = new ArraySerializationSurrogate(type);
            }
            //else if (typeof(IList).IsAssignableFrom(type))
            //{
            //    surrogate = new IListSerializationSurrogate(type);
            //}
            else if (typeof(List<>).Equals(type))
            {
                surrogate = new IListSerializationSurrogate(type);
            }
            else if (typeof(ICompactSerializable).IsAssignableFrom(type))
            {
                surrogate = new ICompactSerializableSerializationSurrogate(type, null);
            }
            else if (typeof(Enum).IsAssignableFrom(type))
            {
                surrogate = new EnumSerializationSurrogate(type);
            }

            if (surrogate == null)
                throw new ArgumentException("No appropriate surrogate found for type " + type.FullName);

            TypeSurrogateSelector.RegisterTypeSurrogate(surrogate);
        }

        /// <summary>
        /// Unregisters the surrogate for the specified type that implements 
        /// <see cref="ICompactSerializable"/> from the system. Used only to unregister
        /// internal types.
        /// <b><u>NOTE: </u></b> <b>CODE COMMENTED, NOT IMPLEMENTED</b>
        /// </summary>
        /// <param name="type">the specified type</param>
        static public void UnregisterCompactType(Type type)
        {
           // throw new NotImplementedException();
            if (type == null) throw new ArgumentNullException("type");
            if (TypeSurrogateSelector.GetSurrogateForTypeStrict(type,null) == null) return;

            if (type.IsArray ||
                //typeof(IDictionary).IsAssignableFrom(type) ||
                //typeof(IList).IsAssignableFrom(type) ||
                typeof(Dictionary<,>).Equals(type) ||
                typeof(List<>).Equals(type) ||
                typeof(ICompactSerializable).IsAssignableFrom(type) ||
                typeof(Enum).IsAssignableFrom(type))
            {
                ISerializationSurrogate surrogate = TypeSurrogateSelector.GetSurrogateForTypeStrict(type,null);
                TypeSurrogateSelector.UnregisterTypeSurrogate(surrogate);
                System.Diagnostics.Debug.WriteLine("Unregistered surrogate for type " + type.FullName);
            }
        }


		/// <summary>
		/// Unregisters the surrogate for the Custom specified type that implements 
		/// <see cref="ICompactSerializable"/> from the system.
		/// </summary>
		/// <param name="type">the specified type</param>
		static public void UnregisterCustomCompactType(Type type,string cacheContext)
		{
            throw new NotImplementedException();
			if (type == null) throw new ArgumentNullException("type");
			if (cacheContext == null) throw new ArgumentException("cacheContext can not be null");

			if (TypeSurrogateSelector.GetSurrogateForTypeStrict(type,cacheContext) == null) return;

			if (type.IsArray ||
                //typeof(IDictionary).IsAssignableFrom(type) ||
                //typeof(IList).IsAssignableFrom(type) ||
                typeof(Dictionary<,>).Equals(type) ||
                typeof(List<>).Equals(type) ||
				typeof(ICompactSerializable).IsAssignableFrom(type) ||
				typeof(Enum).IsAssignableFrom(type))
			{
				ISerializationSurrogate surrogate = TypeSurrogateSelector.GetSurrogateForTypeStrict(type,cacheContext);
				TypeSurrogateSelector.UnregisterTypeSurrogate(surrogate,cacheContext);
				System.Diagnostics.Debug.WriteLine("Unregistered surrogate for type " + type.FullName);
			}
		}

       /// <summary>
       /// Unregisters all the compact types associated with the cache context.
       /// </summary>
       /// <param name="cacheContext">Cache context</param>
        static public void UnregisterAllCustomCompactTypes(string cacheContext)
        {
            if (cacheContext == null) throw new ArgumentException("cacheContext can not be null");

            TypeSurrogateSelector.UnregisterAllSurrogates(cacheContext);
        }
        #endregion
    }
}
