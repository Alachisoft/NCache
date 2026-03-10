using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Runtime
{
    public class NCacheConfigurationOptions
    {
        /// <summary>
        /// Set the install directory of NCache.
        /// </summary>
        public static string InstallDir { get; set; }

        // <summary>
        /// Set the path for client logs.
        /// </summary>
        public static string LogPath { get; set; }
    }
}