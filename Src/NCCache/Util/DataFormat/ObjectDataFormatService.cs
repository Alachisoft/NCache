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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Util;
using System.Collections;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.Caching
{
    internal class ObjectDataFormatService : IDataFormatService
    {
        private CacheRuntimeContext _context;

        public ObjectDataFormatService(CacheRuntimeContext context)
        {
            this._context = context;
        }

        public object GetClientData(object data, ref BitSet flag, LanguageContext languageContext)
        {
            byte[] serializedObject = null;
            try
            {
                switch (languageContext)
                {
                    case LanguageContext.DOTNET:
                        serializedObject = SerializationUtil.SafeSerialize(data,_context.SerializationFormat, _context.SerializationContext, ref flag) as byte[];
                        break;
                }

                if (serializedObject != null && _context.CompressionEnabled)
                    serializedObject = CompressionUtil.Compress(serializedObject, ref flag, _context.CompressionThreshold);
                if (serializedObject != null)
                    return UserBinaryObject.CreateUserBinaryObject(serializedObject, _context.TransactionalPoolManager);
            }
            catch (Exception ex)
            {
                if (_context.NCacheLog != null)
                {
                    if (_context.NCacheLog.IsErrorEnabled)
                        _context.NCacheLog.Error("ObjectDataFormatService.GetClientData()", ex.Message);
                }

            }
            return data;
        }

        public object GetCacheData(object data, BitSet flag)
        {
            byte[] serializedObject = null;

            try
            {
                UserBinaryObject userBinaryObject = null;
                if (data is UserBinaryObject)
                    userBinaryObject = (UserBinaryObject)data;
                else
                    userBinaryObject = UserBinaryObject.CreateUserBinaryObject((ICollection)data, _context.TransactionalPoolManager);

                if (userBinaryObject != null)
                {
                    serializedObject = userBinaryObject.GetFullObject() as byte[];
                    if (_context.CompressionEnabled)
                        serializedObject = CompressionUtil.Decompress(serializedObject, flag);

                    return SerializationUtil.SafeDeserialize(serializedObject, _context.SerializationContext, flag);
                }
            }
            catch (Exception ex)
            {
                if (_context.NCacheLog != null && _context.NCacheLog.IsErrorEnabled)
                {
                    _context.NCacheLog.Error("ObjectDataFormatService.GetCacheData()", ex.Message);
                }
            }
            return data;
        }

        public void GetEntryClone(CacheEntry cacheEntry, out CacheEntry entry, out Array userPayload, out long payLoadSize)
        {
            entry = null; userPayload = null; payLoadSize = 0;
            
            try
            {
                if (cacheEntry != null)
                {
                    entry = cacheEntry.DeepCloneWithoutValue(_context.TransactionalPoolManager);

                    // If 'cacheEntry.Value' is not UserBinaryObject, 'userPayload' is to be null
                    userPayload = (cacheEntry.Value as UserBinaryObject)?.ClonePayload();
                    payLoadSize = cacheEntry.DataSize;
                }
            }
            catch (Exception ex)
            {
                if (_context.NCacheLog != null)
                {
                    if (_context.NCacheLog.IsErrorEnabled)
                        _context.NCacheLog.Error("ObjectDataFormatService.GetCacheData()", ex.Message);
                }
            }
            finally
            {
                if (entry != null)
                    entry.MarkInUse(NCModulesConstants.Global);
               
            }
        }

        /// <summary>
        /// Mainly written for cache data as JSON for backing source. Please do not call this method from anywhere 
        /// other than the derived classes of <see cref="Runtime.Caching.ProviderItemBase"/>.
        /// </summary>
        public T GetCacheData<T>(object data, BitSet flag, UserObjectType userObjectType)
        {
            var userBinaryObject = data as UserBinaryObject;    // Cache Items

            if (userBinaryObject != default(UserBinaryObject))
            {
                var serializedObject = userBinaryObject.GetFullObject() as byte[];

                if (_context.CompressionEnabled)
                    serializedObject = CompressionUtil.Decompress(serializedObject, flag);

                return SerializationUtil.SafeDeserializeOutProc<T>(serializedObject, _context.SerializationContext, flag, true, userObjectType);
            }
            return (T)data;
        }
    }
}
