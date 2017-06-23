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

        private static ISerializationSurrogate nullSurrogate = new NullSerializationSurrogate();
        private static ISerializationSurrogate defaultSurrogate = new ObjectSerializationSurrogate(typeof(object));
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
        static internal ISerializationSurrogate GetSurrogateForTypeStrict(Type type)
        {
			ISerializationSurrogate surrogate = null;
			    surrogate = (ISerializationSurrogate)typeSurrogateMap[type];

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


            RegisterTypeSurrogate(new AverageResultSerializationSurrogate());

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

            ///Generics are not supportorted onwards from 4.1
            //RegisterTypeSurrogate(new GenericIListSerializationSurrogate(typeof(IList<>)));
            RegisterTypeSurrogate(new NullSerializationSurrogate());

            RegisterTypeSurrogate(new IDictionarySerializationSurrogate(typeof(Hashtable)));
            RegisterTypeSurrogate(new IDictionarySerializationSurrogate(typeof(SortedList)));

            ///Generics are not supportorted onwards from 4.1
            RegisterTypeSurrogate(new NullSerializationSurrogate());

			RegisterTypeSurrogate(new SessionStateCollectionSerializationSurrogate(typeof(SessionStateItemCollection)));
            RegisterTypeSurrogate(new SessionStateStaticObjectCollectionSerializationSurrogate(typeof(System.Web.HttpStaticObjectsCollection)));
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
                    throw new ArgumentException("Specified type handle is already registered.");

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
        /// Unregisters all surrogates associalted with the caceh context.
        /// </summary>
        /// <param name="cacheContext"></param>
        static public void UnregisterAllSurrogates(string cacheContext)
        {

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

                typeHandle = short.MinValue;
                RegisterTypeSurrogate(nullSurrogate);
                RegisterTypeSurrogate(defaultSurrogate);
            }
        }

        #endregion
    }
}
