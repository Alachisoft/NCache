// Copyright (c) 2018 Alachisoft
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
using Alachisoft.NCache.Serialization.Surrogates;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Serialization;
namespace Alachisoft.NCache.Serialization
{
    /// <summary>
    /// Provides methods to register <see cref="ICompactSerializable"/> implementations
    /// utilizing available surrogates.
    /// </summary>
    public sealed class CompactFormatterServices
    {
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
        static public void RegisterCompactType(Type type, short typeHandle)
        {
            //registers type as version compatible compact type
            RegisterCompactType(type, typeHandle, true);
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
        static public void RegisterNonVersionCompatibleCompactType(Type type, short typeHandle)
        {
            RegisterCompactType(type, typeHandle, false);
        }

        static private void RegisterCompactType(Type type, short typeHandle, bool versionCompatible)
        {
            if (type == null) throw new ArgumentNullException("type");
            ISerializationSurrogate surrogate = null;

            if ((surrogate = TypeSurrogateSelector.GetSurrogateForTypeStrict(type)) != null)
            {
                //No need to check subHandle since this funciton us not used by DataSharing
                if (surrogate.TypeHandle == typeHandle)
                    return; //Type is already registered with same handle.
                throw new ArgumentException("Type " + type.FullName + "is already registered with different handle");
            }

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
                    surrogate = new VersionCompatibleCompactSerializationSurrogate(type);
                else
                    surrogate = new ICompactSerializableSerializationSurrogate(type);
            }
            else if (typeof(Enum).IsAssignableFrom(type))
            {
                surrogate = new EnumSerializationSurrogate(type);
            }

            if (surrogate == null)
                throw new ArgumentException("No appropriate surrogate found for type " + type.FullName);

            System.Diagnostics.Debug.WriteLine("Registered suurogate for type " + type.FullName);
            TypeSurrogateSelector.RegisterTypeSurrogate(surrogate, typeHandle);
        }


        /// <summary>
       /// Unregisters all the compact types associated with the cache context.
       /// </summary>
       /// <param name="cacheContext">Cache context</param>
        static public void UnregisterAllCustomCompactTypes(string cacheContext)
        {
            if (cacheContext == null) throw new ArgumentException("cacheContext can not be null");

            TypeSurrogateSelector.UnregisterAllSurrogates(cacheContext);
            System.Diagnostics.Debug.WriteLine("Unregister all types " + cacheContext);

        }
        #endregion
    }
}
