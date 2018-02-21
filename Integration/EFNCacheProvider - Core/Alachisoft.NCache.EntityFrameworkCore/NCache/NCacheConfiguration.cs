// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Alachisoft.NCache.EntityFrameworkCore.NCache;
using Alachisoft.NCache.Web.Caching;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Alachisoft.NCache.EntityFrameworkCore
{
    /// <summary>
    /// Class used to configure NCache as a second level cache for entity framework.
    /// </summary>
    public class NCacheConfiguration
    {
        private static string _cacheId = "";
        private static bool _isConfigured = false;
        private static CacheInitParams _initParams;
        private static ILoggerFactory _loggerFactory;
        private static DependencyType _databaseType = DependencyType.Other;

        /// <summary>
        /// Specifies the cache name in which your result sets will be stored.
        /// </summary>
        public static string CacheId => _cacheId;

        /// <summary>
        /// Flag specifying whether provider has been configured or not. True value 
        /// corresponds to the provider being configured and false corresponds to 
        /// otherwise.
        /// </summary>
        public static bool IsConfigured => _isConfigured;

        /// <summary>
        /// Additional parameters used to initialize NCache. Generally used to provide 
        /// security information or to override default NCache parameters.
        /// </summary>
        public static CacheInitParams InitParams => _initParams;

        /// <summary>
        /// Used to inform the cache about the database being used by Entity Framework provider.
        /// While using database dependencies, this option specifies which database dependency 
        /// should be created.
        /// </summary>
        public static DependencyType DatabaseType => _databaseType;


        internal static ILoggerFactory LoggerFactory => _loggerFactory;

        /// <summary>
        /// Configures NCache to be used as a second level cache for entity framework.
        /// </summary>
        /// <param name="cacheId">Cache id that will be used to store result sets.</param>
        /// <param name="databaseType">Database type that will be used when creating database dependencies.</param>
        public static void Configure(string cacheId, DependencyType databaseType)
        {
            _cacheId = cacheId;
            _databaseType = databaseType;

            _isConfigured = true;
        }

        /// <summary>
        /// Configures NCache to be used as a second level cache for entity framework.
        /// </summary>
        /// <param name="cacheId">Cache id that will be used to store result sets.</param>
        /// <param name="databaseType">Database type that will be used when creating database dependencies.</param>
        /// <param name="initParams">Additional parameters used to initialize NCache.</param>
        public static void Configure(string cacheId, DependencyType databaseType, CacheInitParams initParams)
        {
            _initParams = initParams;
            Configure(cacheId, databaseType);
        }

        /// <summary>
        /// Configures logging for Entity framework core provider. Helpful for debugging and getting detailed information in case 
        /// of any errors/exceptions.
        /// By default the Logs are created in NCache's log directory unless specified.
        /// </summary>
        /// <param name="factory">The custom logger factory that will provide the loggers.</param>
        /// <param name="categoryName">specifies the category name of the logger.</param>
        /// <param name="path">corresponds to the custom path where user wants to create the logs. can be null.</param>
        /// <param name="logLevel">Specifies the severity of the event that is to be logged.</param>
        public static void ConfigureLogger(ILoggerFactory factory = default(ILoggerFactory), string categoryName = default(string), string path = default(string), LogLevel logLevel = LogLevel.Debug)
        {
            if (factory == default(ILoggerFactory))
            {
                factory = EFLoggerFactory.Instance;
            }
            _loggerFactory = factory;
            ILogger logger = factory.CreateLogger(categoryName ?? "");

            if (factory is EFLoggerFactory)
            {
                ((EFLogger)logger).Configure(logLevel, path);
                QueryCacheManager.Loggers = new ILogger[] { logger };
            }
            else
            {
                FieldInfo loggersField = logger.GetType().GetField("_loggers", BindingFlags.NonPublic | BindingFlags.Instance);
                QueryCacheManager.Loggers = (ILogger[])loggersField.GetValue(logger);
            }
        }

        /// <summary>
        /// Checks if any of the configured loggers is logging for the specified severity levels or not.
        /// </summary>
        /// <param name="logLevel">The severity level which is to be checked.</param>
        /// <returns>returns true if logging is available for the specified severity level else returns false.</returns>
        public static bool IsLoggerEnabled(LogLevel logLevel)
        {
            if (QueryCacheManager.Loggers != default(ILogger[]))
            {
                foreach (ILogger logger in QueryCacheManager.Loggers)
                {
                    if (logger.IsEnabled(logLevel))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
