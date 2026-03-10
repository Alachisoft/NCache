using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Logger
{
    public class NCacheServiceLogger
    {
        private static NCacheLogger _logger;

        public static NCacheLogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }

        public static void LogError(string message)
        {
            try
            {
                if (_logger != null && _logger.IsErrorEnabled)
                {
                    _logger.Error(message);
                }
            }
            catch (Exception) { }
        }

        public static void LogInfo(string message)
        {
            try
            {
                if (_logger != null && _logger.IsInfoEnabled)
                {
                    _logger.Info(message);
                }
            }
            catch (Exception) { }
        }

        public static void Dispose()
        {
            if (_logger != null)
            {
                _logger.Flush();
                _logger.Close();
            }
        }
    }
}
