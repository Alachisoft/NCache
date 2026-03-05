using System;
using System.Globalization;

namespace Lextm.SharpSnmpLib.Mib
{
    internal static class StatusHelper
    {
        public static Status CreateStatus(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }
            if (text == "current")
            {
                return Status.Current;
            }

            if (text == "deprecated")
            {
                return Status.Deprecated;
            }

            if (text == "obsolete")
            {
                return Status.Obsolete;
            }

            if (text == "mandatory")
            {
                return Status.Mandatory;
            }

            if (text == "optional")
            {
                return Status.Optional;
            }
            
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} is not a valid status text", text), "text");
        }
    }
}