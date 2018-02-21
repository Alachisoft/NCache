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

using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Alachisoft.NCache.EntityFrameworkCore
{
    internal static class DbContextUtils
    {
        internal static object GetPrimaryKeyValue(DbContext context, object entity)
        {
            Microsoft.EntityFrameworkCore.Metadata.IKey key = context.Model.FindEntityType(entity.GetType().FullName).FindPrimaryKey();

            if (key == null)
            {
                return null;
            }

            return key.Properties.Select(p => p.PropertyInfo.GetValue(entity)).FirstOrDefault();
        }

        internal static bool IsEntity(DbContext context, object possibleEntity)
        {
            return IsEntity(context, possibleEntity.GetType());
        }

        internal static bool IsEntity(DbContext context, Type type)
        {
            return context.Model.FindEntityType(type.FullName) != null;
        }
    }
}
