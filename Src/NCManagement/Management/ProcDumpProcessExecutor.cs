using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Management.Management
{
    public class ProcDumpProcessExecutor: CustomProcessExecutorBase
    {
        public ProcDumpProcessExecutor(String command) : base(command,"procdump.exe")
        {
        }
    }
}
