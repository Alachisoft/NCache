using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Runtime.ErrorHandling
{
   internal class ErrorCodes
    {
        public class Json
        {
            public const int ATTRIBUTE_ALREADY_EXISTS = 22500;
            public const int REFERENCE_TO_PARENT = 22501;
            public const int REFERENCE_TO_SELF = 22502;
        }
        public class Common
        {
            public const int DEPENDENCY_KEY_DONT_EXIST = 17512;
        }
    }
}
