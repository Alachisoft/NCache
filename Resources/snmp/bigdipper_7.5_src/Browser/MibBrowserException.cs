using System;
using System.Runtime.Serialization;

namespace Lextm.SharpSnmpLib.Browser
{
    [Serializable]
    public sealed class BrowserException : Exception
    {
        public BrowserException() 
        { 
        }
        
        public BrowserException(string message) : base(message) 
        { 
        }
        
        public BrowserException(string message, Exception inner) : base(message, inner) 
        { 
        }

        private BrowserException(SerializationInfo info, StreamingContext context)
            : base(info, context) 
        { 
        }
    }
}