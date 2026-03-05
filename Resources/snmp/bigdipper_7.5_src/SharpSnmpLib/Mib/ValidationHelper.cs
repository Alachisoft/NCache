using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace Lextm.SharpSnmpLib.Mib
{
    internal static class ValidationHelper
    {
        internal static readonly ILog Logger = LogManager.GetLogger(typeof (ValidationHelper));

        internal static bool ValidateParent(this IEntity entity, IEnumerable<IConstruct> knownConstructs, string fileName)
        {
            // IMPORTANT: quick fix for default constructs (they are assumed as valid)
            if (entity.Parent == "iso")
            {
                return true;
            }

            if (entity.Parent == "iso.org(3).dod(6)")
            {
                return true;
            }

            if (entity.Parent == "ccitt")
            {
                return true;
            }

            if (knownConstructs.Any(construct => String.CompareOrdinal(construct.Name, entity.Parent) == 0))
            {
                return true;
            }

            var builder = new StringBuilder();
            if (String.IsNullOrEmpty(fileName))
            {
                builder.Append("error S0003 : ");
            }
            else
            {
                builder.AppendFormat("{0} : error S0003 : ", fileName);
            }

            builder.AppendFormat("{0} is not defined", entity.Parent);
            Logger.Error(builder.ToString());

            // TODO: make this validation strict later.
            return true;
        }
    }
}
