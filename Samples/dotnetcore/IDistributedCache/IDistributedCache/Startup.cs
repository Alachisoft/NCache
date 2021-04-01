using System;
using System.Configuration;
using System.Text;
using Alachisoft.NCache.Web.SessionState;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SampleApp
{
    public class Startup
    {
        private readonly IConfiguration _config;
        private readonly IHostingEnvironment _hostContext;

        public Startup(IConfiguration config, IHostingEnvironment hostContext)
        {
            _config = config;
            _hostContext = hostContext;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            #region Option1
            // Reading NCacheSettings using appsettings.json
            services.AddNCacheDistributedCache(_config.GetSection("NCacheSettings"));
            #endregion

            #region Option2
            // Reading NCacheSettings using hardcoded values
            //services.AddNCacheDistributedCache(options =>
            //    {
            //        options.CacheName = "myPartitionedCache";
            //        options.EnableLogs = true;
            //        options.ExceptionsEnabled = true;
            //    });
            #endregion

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        #region snippet_Configure
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, 
            IApplicationLifetime lifetime, IDistributedCache cache)
        {
            lifetime.ApplicationStarted.Register(() =>
            {
                var currentTimeUTC = DateTime.UtcNow.ToString();
                byte[] encodedCurrentTimeUTC = Encoding.UTF8.GetBytes(currentTimeUTC);
                var options = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(20));
                cache.Set("cachedTimeUTC", encodedCurrentTimeUTC, options);
            });
        #endregion

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();
            app.UseMvc();
        }
    }
}
