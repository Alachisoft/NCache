using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Automation
{
   public interface IConfiguration
    {
        void InitializeCommandLinePrameters(string[] args);
        bool ValidateParameters();
    }
}
