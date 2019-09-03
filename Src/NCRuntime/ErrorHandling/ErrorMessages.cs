using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Runtime.ErrorHandling
{
   internal class ErrorMessages
    {
        private static IDictionary<int, string> _errorMessageMap = new Dictionary<int, string>();

        static ErrorMessages()
        {
            _errorMessageMap.Add(ErrorCodes.Json.ATTRIBUTE_ALREADY_EXISTS, "An attribute with the same name aleady exists.");
            _errorMessageMap.Add(ErrorCodes.Json.REFERENCE_TO_PARENT, "Reference to parent at nested level detected.");
            _errorMessageMap.Add(ErrorCodes.Common.DEPENDENCY_KEY_DONT_EXIST, "One of the dependency keys does not exist.");
            _errorMessageMap.Add(ErrorCodes.Json.REFERENCE_TO_SELF, "'{0}' cannot contain an attribute that is a reference to self.");
        }
        public static string GetErrorMessage(int errorCode, params string[] parameters)
        {
            string errormessage = "exception";
            //return errormessage;
            return ResolveError(errorCode, parameters);
        }

        internal static string ResolveError(int errorCode, params string[] parameters)
        {
            string message;
            if (_errorMessageMap.TryGetValue(errorCode, out message))
            {
                if (parameters == null || parameters.Length == 0)
                    return message;
                return String.Format(message, parameters);
            }
            return String.Format("Missing error message for code ({0}) in error to exception map", new object[] { errorCode });
        }
    }
}
