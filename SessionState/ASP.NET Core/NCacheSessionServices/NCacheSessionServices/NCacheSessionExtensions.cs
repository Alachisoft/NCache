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
        public static IServiceCollection AddNCacheSession(this IServiceCollection services,
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

            services.AddOptions().Configure<NCacheSessionConfiguration>(configSection);
            return services.AddService();
        }

        private static IServiceCollection AddService(this IServiceCollection services)
        {
            //services.AddDataProtection();
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
