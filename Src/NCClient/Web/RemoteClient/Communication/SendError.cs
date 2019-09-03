using System;

namespace Alachisoft.NCache.Client
{
    internal enum ErrorType : byte
    {
        ActivityBlocked,
        ConnectionException,
        Exception
    }

    internal class SendError
    {
        internal ErrorType Type { get; }
        internal Exception Exception { get; }

        internal SendError(ErrorType type, Exception exception)
        {
            Type = type;
            Exception = exception;
        }
    }
}
