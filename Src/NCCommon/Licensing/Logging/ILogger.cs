using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Common.Licensing.Logging
{
    public interface ILogger
    {
        void LogInformation(String message);
        void Debug(String message);
        void LogError(String message);

    }
}
