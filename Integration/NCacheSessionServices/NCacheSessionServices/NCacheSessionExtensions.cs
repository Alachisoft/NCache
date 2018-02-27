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
// limitations under the License

using System;
using Alachisoft.NCache.Web.SessionState.Configuration;
using Alachisoft.NCache.Web.SessionState.Interface;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Web.SessionState.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alachisoft.NCache.Web.SessionState
{
    public static class NCacheSessionExtensions
    {
        /// <summary>
        /// Initializes NCache Session Storage Services configuration from a configuration section
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configSection">The configuration section that contains the configuration</param>
        /// <returns></returns>
        public static IServiceCollection SetNCacheSessionConfiguration(this IServiceCollection services,
            IConfigurationSection configSection)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configSection == null)
            {
                throw new ArgumentNullException(nameof(configSection));
            }

            return services.AddOptions().Configure<NCacheSessionConfiguration>(configSection);
            
        }

        /// <summary>
        /// Initializes NCache Session Storage Services configuration from a Json configuration file.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configVirtualPath">The virtual path to the Json configuration file</param>
        /// <returns></returns>
        public static IServiceCollection SetNCacheSessionConfiguration(this IServiceCollection services,
            string configVirtualPath)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (string.IsNullOrEmpty(configVirtualPath))
            {
                throw new ArgumentNullException(nameof(configVirtualPath));
            }

            return
                services.AddOptions()
                    .Configure<NCacheSessionConfiguration>(
                        new ConfigurationBuilder().AddJsonFile(configVirtualPath, false, true).Build());

        }

        /// <summary>
        /// Initializes NCache Session Storage Services
        /// </summary>
        /// <returns></returns>
        public static IServiceCollection AddNCacheSession(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddService();

        }

        private static IServiceCollection AddService(this IServiceCollection services)
        {
            services.AddDataProtection();
            return
                services.AddSingleton<ISessionKeyGenerator, SessionKeyGenerator>()
                    .AddSingleton<ISessionKeyManager, SessionKeyManager>()
                    .AddSingleton<ISessionStoreService, NCacheSessionStoreService>();
        }

        /// <summary>
        /// Initializes NCache Session Storage Services
        /// </summary>
        /// <param name="services">The service collection container</param>
        /// <param name="configure">Action to configure an NCache Session Storage Service Configuration</param>
        /// <returns></returns>
        public static IServiceCollection AddNCacheSession(this IServiceCollection services,
            Action<NCacheSessionConfiguration> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            services.Configure(configure);
            return services.AddService();
        }

        /// <summary>
        /// Adds a session middleware to the application flow. 
        /// </summary>
        /// <returns></returns>
        public static IApplicationBuilder UseNCacheSession(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseMiddleware<NCacheSessionMiddleware>();
        }


        /// <summary>
        /// Adds the NCache Cache as the standard distibuted cache to be used by sessions
        /// </summary>
        /// <returns></returns>
        public static IServiceCollection AddNCacheDistributedCache(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddSingleton<IDistributedCache, NCacheDistributedCache>();
        }

        /// <summary>
        /// Adds the NCache Cache whose name is specified in the configuration as the standard distibuted cache to be used by sessions
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IServiceCollection AddNCacheDistributedCache(this IServiceCollection services,
            Action<NCacheSessionConfiguration> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            services.Configure(configure);
            return services.AddSingleton<NCacheDistributedCache>();
        }

        /// <summary>
        /// Sets a value against the specified key in the session. 
        /// </summary>
        /// <param name="session">The session object</param>
        /// <param name="key">Key against which the value is required</param>
        /// <param name="value">The value against the key which is serialized using NCache Compact Serialization</param>
        public static void Set(this ISession session, string key, object value)
        {
            //Note: This might require registering the compact types first if the user decides to use these extensions on the default session object
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var data = CompactBinaryFormatter.ToByteBuffer(value, string.Empty);
            session.Set(key, data);
        }

        /// <summary>
        /// Tries to get the value against the specified key from session. 
        /// </summary>
        /// <param name="session">The session object</param>
        /// <param name="key">Key against which the value is required</param>
        /// <param name="value">The deserialized value against the key</param>
        /// <returns>A boolean that specifies whether the operation was successful or not. </returns>
        public static bool TryGetValue(this ISession session, string key, out object value)
        {
            value = null;

            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            byte[] data;
            if (session.TryGetValue(key, out data))
            {
                try
                {
                    value = CompactBinaryFormatter.FromByteBuffer(data, string.Empty);
                }
                catch
                {
                    return false;
                }
                return true;
            }

            return false;
        }
    }
}
