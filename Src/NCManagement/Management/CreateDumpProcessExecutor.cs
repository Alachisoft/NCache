using Alachisoft.NCache.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Alachisoft.NCache.Management.Management
{
    public class CreateDumpProcessExecutor : CustomProcessExecutorBase
    {
        public CreateDumpProcessExecutor(String command) : base(command, "createdump")
        {
        }

        /// <summary>
        /// Override to provide Linux-specific application path.
        /// </summary>
        /// <returns>Path to the Linux dump creation tool.</returns>
        protected override string GetApplicationPath()
        {
            string basePath = Path.Combine(AppUtil.InstallDir, "bin");
            string webPath = Path.Combine(basePath, "tools", "web");
            return Path.Combine(webPath, _toolName);
        }
    }
}
