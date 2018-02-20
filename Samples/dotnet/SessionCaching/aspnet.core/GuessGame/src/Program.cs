using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Alachisoft.Samples.GuessGameCore
{
    public class Program
    {
        /// <summary>
        /// A guessing game to demonstrate the usage of NCache Session Services in ASP.NET Core
        /// Open up the appsettings.json and replace "MyCache" against "CacheName" with your already running cache
        /// </summary>
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://0.0.0.0:5000")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
