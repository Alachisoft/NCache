using System;
using Alachisoft.NCache.Caching.Distributed.Configuration;
using Alachisoft.NCache.Serialization.Formatters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alachisoft.NCache.Caching.Distributed
{
    public static class NCacheSessionExtensions
    {
        /// <summary>
        /// Adds the NCache Cache whose name is specified in the configuration as the standard distibuted cache to be used by sessions
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IServiceCollection AddNCacheDistributedCache(this IServiceCollection services,
            Action<NCacheConfiguration> configure)
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
            return services.AddSingleton<IDistributedCache, NCacheDistributedCache>();
        }
        
        /// <summary>
        /// Adds the NCache Cache whose name is specified in the configuration as the standard distibuted cache to be used by sessions
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configSection">The virtual path to the Json configuration file</param>
        /// <returns></returns>
        public static IServiceCollection AddNCacheDistributedCache(this IServiceCollection services,
          IConfiguration configSection)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configSection == null)
            {
                throw new ArgumentNullException(nameof(configSection));
            }

            services.AddOptions().Configure<NCacheConfiguration>(configSection);
            return services.AddSingleton<IDistributedCache, NCacheDistributedCache>();
        }
    }
}
