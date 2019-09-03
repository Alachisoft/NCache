using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace SessionTest
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();
            services.SetNCacheSessionConfiguration(Configuration.GetSection("NCacheSessions"));
            services.AddNCacheSession();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseNCacheSession();
            app.Map("/session", subApp =>
            {
                subApp.Run(async context =>
                {
                    int visits = 0;
                    visits = context.Session.GetInt32("visits") ?? 0;
                    context.Session.SetInt32("visits", ++visits);
                    await context.Response.WriteAsync("Counting: You have visited our page this many times: " + visits);
                });
            });

            app.Run(async context =>
            {
                int visits = 0;
                visits = context.Session.GetInt32("visits") ?? 0;
                await context.Response.WriteAsync("<html><body>");
                if (visits == 0)
                {
                    await context.Response.WriteAsync("Your session has not been established.<br>");
                    await context.Response.WriteAsync(DateTime.Now + "<br>");
                    await context.Response.WriteAsync("<a href=\"/session\">Establish session</a>.<br>");
                }
                else
                {
                    context.Session.SetInt32("visits", ++visits);
                    await context.Response.WriteAsync("Your session was located, you've visited the site this many times: " + visits);
                }
                await context.Response.WriteAsync("</body></html>");
            });
        }
    }
}
