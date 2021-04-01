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
using System.Web.SessionState;
using System.Collections.Specialized;
using System.Reflection;

using Alachisoft.NCache.Serialization.Surrogates;
using System.Collections.Generic;

namespace Alachisoft.NCache.Serialization
{
    /// <summary>
    /// Provides the common type identification system. Takes care of registering type surrogates
    /// and type handles. Provides methods to register <see cref="ICompactSerializable"/> implementations
    /// utilizing the built-in surrogate for <see cref="ICompactSerializable"/>.
    /// </summary>
    public sealed class TypeSurrogateSelector
    {
        private static readonly Type s_typeofList = typeof(List<>);        

        private static IDictionary typeSurrogateMap = Hashtable.Synchronized(new Hashtable());
        private static IDictionary handleSurrogateMap = Hashtable.Synchronized(new Hashtable());
		private static IDictionary userTypeSurrogateMap = Hashtable.Synchronized(new Hashtable());
		private static IDictionary userTypeHandleSurrogateMap = Hashtable.Synchronized(new Hashtable());

        private static ISerializationSurrogate nullSurrogate = new NullSerializationSurrogate();
        private static ISerializationSurrogate defaultSurrogate = new ObjectSerializationSurrogate(typeof(object), null);
        private static ISerializationSurrogate defaultArraySurrogate = new ObjectArraySerializationSurrogate();

        private static short typeHandle = short.MinValue;
		private static short CUSTOM_TYPE_RANGE = 1000;
        /// <summary>
        /// Static constructor registers built-in surrogates with the system.
        /// </summary>
        static TypeSurrogateSelector()
        {
            RegisterTypeSurrogate(nullSurrogate);
            RegisterTypeSurrogate(defaultSurrogate);
            RegisterTypeSurrogate(defaultArraySurrogate);

            RegisterBuiltinSurrogates();
        }

        /// <summary>
        /// Finds and returns an appropriate <see cref="ISerializationSurrogate"/> for the given
        /// object.
        /// </summary>
        /// <param name="graph">specified object</param>
        /// <returns><see cref="ISerializationSurrogate"/> object</returns>
        static internal ISerializationSurrogate GetSurrogateForObject(object graph, string cacheContext)
        {
            if (graph == null)
                return nullSurrogate;
            Type type = null;

            if (graph is ArrayList)
                type = typeof(ArrayList);
            else if (graph is Hashtable)
                type = typeof(Hashtable);
            else if (graph is SortedList)
                type = typeof(SortedList);
            else if (graph is Common.DataStructures.Clustered.HashVector)
                type = typeof(Common.DataStructures.Clustered.HashVector);
            else if (graph is Common.DataStructures.Clustered.ClusteredArrayList)
                type = typeof(Common.DataStructures.Clustered.ClusteredArrayList);
            else if (graph.GetType().IsGenericType && typeof(List<>).Equals(graph.GetType().GetGenericTypeDefinition()) && graph.GetType().FullName.Contains("System.Collections.Generic"))
                ///Its IList<> but see if it is a user defined type that derived from IList<>
                type = typeof(IList<>);
            else if (graph.GetType().IsGenericType && typeof(Dictionary<,>).Equals(graph.GetType().GetGenericTypeDefinition()) && graph.GetType().FullName.Contains("System.Collections.Generic"))
                type = typeof(IDictionary<,>);            
            else if (graph.GetType().IsArray && UserTypeSurrogateExists(graph.GetType().GetElementType(), cacheContext))
                type = (new ObjectArraySerializationSurrogate()).ActualType;
            else
                type = graph.GetType();

            return GetSurrogateForType(type, cacheContext);
        }

        private static bool UserTypeSurrogateExists(Type type, string cacheContext)
        {
            bool exists = false;

            if (cacheContext != null)
            {
                Hashtable userTypeMap = (Hashtable)userTypeSurrogateMap[cacheContext];
                if (userTypeMap != null)
                    exists = userTypeMap.Contains(type);
            }

            return exists;
        }

        /// <summary>
        /// Finds and returns an appropriate <see cref="ISerializationSurrogate"/> for the given
        /// type if not found returns defaultSurrogate
        /// </summary>
        /// <param name="type">specified type</param>
        /// <param name="cacheContext">CacheName, incase of null only builting registered map is searched</param>
        /// <returns><see cref="ISerializationSurrogate"/> object</returns>
        static public ISerializationSurrogate GetSurrogateForType(Type type,string cacheContext)
        {
            ISerializationSurrogate surrogate = (ISerializationSurrogate)typeSurrogateMap[type];

			if(surrogate == null && cacheContext != null)
			{
				Hashtable userTypeMap = (Hashtable)userTypeSurrogateMap[cacheContext.ToLower()];
				if(userTypeMap != null)
					surrogate = (ISerializationSurrogate)userTypeMap[type];
			}
			
			if (surrogate == null)
				surrogate = defaultSurrogate;

            return surrogate;
        }

        /// <summary>
        /// Finds and returns <see cref="ISerializationSurrogate"/> for the given
        /// type if not found returns null
        /// </summary>
        /// <param name="type">specified type</param>
        /// <returns><see cref="ISerializationSurrogate"/> object</returns>
        static internal ISerializationSurrogate GetSurrogateForTypeStrict(Type type,string cacheContext)
        {
			ISerializationSurrogate surrogate = null;
			    surrogate = (ISerializationSurrogate)typeSurrogateMap[type];

			if(surrogate == null && cacheContext != null)
			{
				Hashtable userTypeMap = (Hashtable)userTypeSurrogateMap[cacheContext];
				if(userTypeMap != null)
					surrogate = (ISerializationSurrogate)userTypeMap[type];
			}

            //For List and Dictionary used in any Generic class produce problems which leads to bud id 2905, so here is the fix
            if (surrogate == null && (type.FullName == typeof(List<>).FullName || type.FullName == typeof(Dictionary<,>).FullName))
                surrogate = CheckForListAndDictionaryTypes(type, cacheContext);

			return surrogate;
        }

        /// <summary>
        /// this method will filter out any IList or IDictionary surrogate if already exists in userTypeMap
        /// </summary>
        /// <param name="type"></param>
        /// <param name="cacheContext"></param>
        /// <returns></returns>
        static internal ISerializationSurrogate CheckForListAndDictionaryTypes(Type type, string cacheContext)
        {
            
            ISerializationSurrogate surrogate = null;
            if (cacheContext != null)
            {
                Hashtable userTypeMap = (Hashtable)userTypeSurrogateMap[cacheContext];
                if (userTypeMap != null)
                {
                    Type listOrDictionaryType = null;
                    IDictionaryEnumerator ide = userTypeMap.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        listOrDictionaryType = (Type)ide.Key;
                        if (listOrDictionaryType != null && listOrDictionaryType.FullName == typeof(IList<>).FullName && type.FullName == typeof(List<>).FullName)
                        {
                            surrogate = (ISerializationSurrogate)userTypeMap[listOrDictionaryType];
                            break;
                        }
                        if (listOrDictionaryType != null && listOrDictionaryType.FullName == typeof(IDictionary<,>).FullName && type.FullName == typeof(Dictionary<,>).FullName)
                        {
                            surrogate = (ISerializationSurrogate)userTypeMap[listOrDictionaryType];
                            break;
                        }
                    }                    
                }
            }
            return surrogate;
        }

        /// <summary>
        /// Finds and returns an appropriate <see cref="ISerializationSurrogate"/> for the given
        /// type handle.
        /// </summary>
        /// <param name="handle">type handle</param>
        /// <returns><see cref="ISerializationSurrogate"/>Object otherwise returns null if typeHandle is base Handle if Portable</returns>
        static internal ISerializationSurrogate GetSurrogateForTypeHandle(short handle,string cacheContext)
        {
			ISerializationSurrogate surrogate = null;
			if(handle < CUSTOM_TYPE_RANGE)
				surrogate = (ISerializationSurrogate)handleSurrogateMap[handle];
            else
            {
                Hashtable userTypeMap = (Hashtable)userTypeHandleSurrogateMap[cacheContext.ToLower()];
                if (userTypeMap != null)
                    if (userTypeMap.Contains(handle))
                    {
                        if (userTypeMap[handle] is ISerializationSurrogate)
                            surrogate = (ISerializationSurrogate)userTypeMap[handle];
                        else
                            return null; 
                    }
            }

            if (surrogate == null)
                surrogate = defaultSurrogate;
            return surrogate;
        }


        /// <summary>
        /// Finds and returns an appropriate <see cref="ISerializationSurrogate"/> for the given
        /// type handle.
        /// </summary>
        /// <param name="handle">type handle</param>
        /// <returns><see cref="ISerializationSurrogate"/>Object otherwise returns null if typeHandle is base Handle if Portable</returns>
        static internal ISerializationSurrogate GetSurrogateForSubTypeHandle(short handle, short subHandle, string cacheContext)
        {
            ISerializationSurrogate surrogate = null;
            Hashtable userTypeMap = (Hashtable)userTypeHandleSurrogateMap[cacheContext];
            if (userTypeMap != null && userTypeMap[handle] != null)
            {
                surrogate = (ISerializationSurrogate)((Hashtable)userTypeMap[handle])[subHandle];
                if (surrogate == null && ((Hashtable)userTypeMap[handle]).Count > 0)
                {
                    IDictionaryEnumerator surr = (IDictionaryEnumerator)((Hashtable)userTypeMap[handle]).Values.GetEnumerator();
                    surr.MoveNext();
                    surrogate = (ISerializationSurrogate)surr.Value;
                }
            }

            if (surrogate == null)
                surrogate = defaultSurrogate;
            return surrogate;
        }


        #region /       ISerializationSurrogate registration specific        /

        /// <summary>
        /// Registers built-in surrogates with the system.
        /// </summary>
        static public void RegisterBuiltinSurrogates()
        {
			RegisterTypeSurrogate(new BooleanSerializationSurrogate());
            RegisterTypeSurrogate(new ByteSerializationSurrogate());
            RegisterTypeSurrogate(new CharSerializationSurrogate());
            RegisterTypeSurrogate(new SingleSerializationSurrogate());
            RegisterTypeSurrogate(new DoubleSerializationSurrogate());
            RegisterTypeSurrogate(new DecimalSerializationSurrogate());
            RegisterTypeSurrogate(new Int16SerializationSurrogate());
            RegisterTypeSurrogate(new Int32SerializationSurrogate());
            RegisterTypeSurrogate(new Int64SerializationSurrogate());
            RegisterTypeSurrogate(new StringSerializationSurrogate());
            RegisterTypeSurrogate(new DateTimeSerializationSurrogate());

            RegisterTypeSurrogate(new NullSerializationSurrogate());
            RegisterTypeSurrogate(new BooleanArraySerializationSurrogate());
            RegisterTypeSurrogate(new ByteArraySerializationSurrogate());
            RegisterTypeSurrogate(new CharArraySerializationSurrogate());
            RegisterTypeSurrogate(new SingleArraySerializationSurrogate());
            RegisterTypeSurrogate(new DoubleArraySerializationSurrogate());
            RegisterTypeSurrogate(new Int16ArraySerializationSurrogate());
            RegisterTypeSurrogate(new Int32ArraySerializationSurrogate());
            RegisterTypeSurrogate(new Int64ArraySerializationSurrogate());
            RegisterTypeSurrogate(new StringArraySerializationSurrogate());


           

            //End of File for Java, ie denotes further types are not supported in java
            RegisterTypeSurrogate(new EOFJavaSerializationSurrogate());
            //End of File for Net, provided by Java ie it denotes not supported in Dot Net
            RegisterTypeSurrogate(new EOFNetSerializationSurrogate());
            //Skip this value Surrogate
            RegisterTypeSurrogate(new SkipSerializationSurrogate());



            RegisterTypeSurrogate(new DecimalArraySerializationSurrogate());
            RegisterTypeSurrogate(new DateTimeArraySerializationSurrogate());
            RegisterTypeSurrogate(new GuidArraySerializationSurrogate());
            RegisterTypeSurrogate(new SByteArraySerializationSurrogate());
            RegisterTypeSurrogate(new UInt16ArraySerializationSurrogate());
            RegisterTypeSurrogate(new UInt32ArraySerializationSurrogate());
            RegisterTypeSurrogate(new UInt64ArraySerializationSurrogate());


            RegisterTypeSurrogate(new GuidSerializationSurrogate());
            RegisterTypeSurrogate(new SByteSerializationSurrogate());
            RegisterTypeSurrogate(new UInt16SerializationSurrogate());
            RegisterTypeSurrogate(new UInt32SerializationSurrogate());
            RegisterTypeSurrogate(new UInt64SerializationSurrogate());

            RegisterTypeSurrogate(new ArraySerializationSurrogate(typeof(Array)));
            RegisterTypeSurrogate(new IListSerializationSurrogate(typeof(ArrayList)));

            RegisterTypeSurrogate(new NullSerializationSurrogate());

            RegisterTypeSurrogate(new IDictionarySerializationSurrogate(typeof(Hashtable)));
            RegisterTypeSurrogate(new IDictionarySerializationSurrogate(typeof(SortedList)));


            RegisterTypeSurrogate(new NullSerializationSurrogate());


			RegisterTypeSurrogate(new SessionStateCollectionSerializationSurrogate(typeof(SessionStateItemCollection), null));
            RegisterTypeSurrogate(new SessionStateStaticObjectCollectionSerializationSurrogate(typeof(System.Web.HttpStaticObjectsCollection), null));
            RegisterTypeSurrogate(new NullableArraySerializationSurrogate<Boolean>());
            RegisterTypeSurrogate(new NullableArraySerializationSurrogate<Byte>());
            RegisterTypeSurrogate(new NullableArraySerializationSurrogate<Char>());
            RegisterTypeSurrogate(new NullableArraySerializationSurrogate<Single>());
            RegisterTypeSurrogate(new NullableArraySerializationSurrogate<Double>());
            RegisterTypeSurrogate(new NullableArraySerializationSurrogate<Decimal>());
            RegisterTypeSurrogate(new NullableArraySerializationSurrogate<Int16>());
            RegisterTypeSurrogate(new NullableArraySerializationSurrogate<Int32>());
            RegisterTypeSurrogate(new NullableArraySerializationSurrogate<Int64>());
            RegisterTypeSurrogate(new NullableArraySerializationSurrogate<DateTime>());
            RegisterTypeSurrogate(new NullableArraySerializationSurrogate<Guid>());
            RegisterTypeSurrogate(new NullableArraySerializationSurrogate<SByte>());
            RegisterTypeSurrogate(new NullableArraySerializationSurrogate<UInt16>());
            RegisterTypeSurrogate(new NullableArraySerializationSurrogate<UInt32>());
            RegisterTypeSurrogate(new NullableArraySerializationSurrogate<UInt64>());

            RegisterTypeSurrogate(new IListSerializationSurrogate(typeof(Common.DataStructures.Clustered.ClusteredArrayList)));
            RegisterTypeSurrogate(new IDictionarySerializationSurrogate(typeof(Common.DataStructures.Clustered.HashVector)));

            RegisterTypeSurrogate(new IPAddressSerializationSurrogate());
            RegisterTypeSurrogate(new IListSerializationSurrogate(typeof(System.Net.IPAddress)));
        }

        /// <summary>
        /// Registers the specified <see cref="ISerializationSurrogate"/> with the system.
        /// </summary>
        /// <param name="surrogate">specified surrogate</param>
        /// <returns>false if the surrogated type already has a surrogate</returns>
        static public bool RegisterTypeSurrogate(ISerializationSurrogate surrogate)
        {
            for (; ; )
            {
                try
                {
                    return RegisterTypeSurrogate(surrogate, ++typeHandle);
                }
                catch (ArgumentException) { }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Registers the specified <see cref="ISerializationSurrogate"/> with the given type handle.
        /// Gives more control over the way type handles are generated by the system and allows the 
        /// user to supply *HARD* handles for better interoperability among applications.
        /// </summary>
        /// <param name="surrogate">specified surrogate</param>
        /// <param name="typeHandle">specified HARD handle for type</param>
        /// <returns>false if the surrogated type already has a surrogate</returns>
        static public bool RegisterTypeSurrogate(ISerializationSurrogate surrogate, short typehandle)
        {
            if (surrogate == null) throw new ArgumentNullException("surrogate");
            lock (typeSurrogateMap.SyncRoot)
            {
                if (handleSurrogateMap.Contains(typehandle))
                    throw new ArgumentException(string.Format("Specified type handle {0} is already registered.", typehandle));

                if (!typeSurrogateMap.Contains(surrogate.ActualType))
                {
                    surrogate.TypeHandle = typehandle;
                    typeSurrogateMap.Add(surrogate.ActualType, surrogate);
                    handleSurrogateMap.Add(surrogate.TypeHandle, surrogate);
                    return true;
                }
            }
            return false;
        }


		/// <summary>
		/// Registers the specified <see cref="ISerializationSurrogate"/> with the given type handle.
		/// Gives more control over the way type handles are generated by the system and allows the 
		/// user to supply *HARD* handles for better interoperability among applications.
		/// </summary>
		/// <param name="surrogate">specified surrogate</param>
		/// <param name="typeHandle">specified HARD handle for type</param>
        /// <param name="cacheContext">Cache Name</param>
        /// <param name="portable">if Data Sharable class; includes Versioning and portablity</param>
        /// <param name="subTypeHandle">if Portable this should not be 0, surrogate is registered by this handle if portability is true</param>
        /// <param name="typehandle">Base TypeHandle provided by config, Surrogate is registered in reference to this handle if portability is false else it holds reference to subTypeHandles</param>
		/// <returns>false if the surrogated type already has a surrogate</returns>
		static public bool RegisterTypeSurrogate(ISerializationSurrogate surrogate, short typehandle,string cacheContext, short subTypeHandle, bool portable)
		{
			if (surrogate == null) throw new ArgumentNullException("surrogate");
			lock (typeSurrogateMap.SyncRoot)
			{ 
				if(cacheContext != null)
				{
					Hashtable userTypeHandleMap = (Hashtable)userTypeHandleSurrogateMap[cacheContext];
					if(userTypeHandleMap == null)
					{
						userTypeHandleMap = new Hashtable();
						userTypeHandleSurrogateMap.Add(cacheContext,userTypeHandleMap);
					}

                    if (portable)
                    {
                        if (userTypeHandleMap.Contains(typehandle))
                        {
                            if(((Hashtable)userTypeHandleMap[typehandle]).Contains(subTypeHandle))
                                throw new ArgumentException("Specified sub-type handle is already registered.");
                        }
                    }
                    else
                    {
                        if (userTypeHandleMap.Contains(typehandle))
                            throw new ArgumentException(string.Format("Specified type handle {0} is already registered.", typehandle));
                    }


					Hashtable userTypeMap = (Hashtable)userTypeSurrogateMap[cacheContext];
					if(userTypeMap == null)
					{
						userTypeMap = new Hashtable();
						userTypeSurrogateMap.Add(cacheContext,userTypeMap);
					}

					if (!userTypeMap.Contains(surrogate.ActualType))
					{
                        if (portable)
                        {
                            //Surrogate will write the subhandle itself when the write method is called
                            surrogate.TypeHandle = typehandle;
                            surrogate.SubTypeHandle = subTypeHandle;
                            if (!userTypeHandleMap.Contains(typehandle) || userTypeHandleMap[typehandle] == null)
                                userTypeHandleMap[typehandle] = new Hashtable();

                            ((Hashtable)userTypeHandleMap[typehandle]).Add(subTypeHandle, surrogate);

                            userTypeMap.Add(surrogate.ActualType, surrogate);
                            return true;
                        }


                        surrogate.TypeHandle = typehandle;
                        userTypeMap.Add(surrogate.ActualType, surrogate);
                        userTypeHandleMap.Add(surrogate.TypeHandle, surrogate);
						return true;
					}
				}
			}

			return false;
		}

        /// <summary>
        /// Unregisters the specified <see cref="ISerializationSurrogate"/> from the system.
        /// <b><u>NOTE: </u></b> <b>CODE COMMENTED, NOT IMPLEMENTED</b>
        /// </summary>
        /// <param name="surrogate">specified surrogate</param>
        static public void UnregisterTypeSurrogate(ISerializationSurrogate surrogate)
        {
            if (surrogate == null) throw new ArgumentNullException("surrogate");
            lock (typeSurrogateMap.SyncRoot)
            {
                typeSurrogateMap.Remove(surrogate.ActualType);
                handleSurrogateMap.Remove(surrogate.TypeHandle);
            }
        }


        /// <summary>
        /// <b><u>NOTE: </u></b> <b>CODE COMMENTED, NOT IMPLEMENTED</b>
        /// </summary>
        /// <param name="surrogate"></param>
        /// <param name="cacheContext"></param>
		static public void UnregisterTypeSurrogate(ISerializationSurrogate surrogate,string cacheContext)
		{
           
		}

        /// <summary>
        /// Unregisters all surrogates associalted with the caceh context.
        /// </summary>
        /// <param name="cacheContext"></param>
        static public void UnregisterAllSurrogates(string cacheContext)
        {
             lock (typeSurrogateMap.SyncRoot)
            {
                if (cacheContext != null)
                {
                    if (userTypeHandleSurrogateMap.Contains(cacheContext))
                        userTypeHandleSurrogateMap.Remove(cacheContext);

                    if (userTypeSurrogateMap.Contains(cacheContext))
                        userTypeSurrogateMap.Remove(cacheContext);
                }
            }
        }
        /// <summary>
        /// Unregisters all surrogates, except null and default ones.
        /// </summary>
        static public void UnregisterAllSurrogates()
        {
            lock (typeSurrogateMap.SyncRoot)
            {
                typeSurrogateMap.Clear();
                handleSurrogateMap.Clear();
				userTypeHandleSurrogateMap.Clear();
				userTypeSurrogateMap.Clear();

                typeHandle = short.MinValue;
                RegisterTypeSurrogate(nullSurrogate);
                RegisterTypeSurrogate(defaultSurrogate);
            }
        }

        #endregion
    }
}
