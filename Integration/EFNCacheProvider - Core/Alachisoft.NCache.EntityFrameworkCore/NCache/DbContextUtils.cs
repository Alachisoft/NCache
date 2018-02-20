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
