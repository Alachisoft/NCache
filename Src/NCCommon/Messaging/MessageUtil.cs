using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Alachisoft.NCache.Common
{
   public class MessageUtil
    {
      


        public static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
             Replace("\\*", ".*").
             Replace("\\[", "[").
             Replace("\\?", ".") + "$";
        }



    }
}
