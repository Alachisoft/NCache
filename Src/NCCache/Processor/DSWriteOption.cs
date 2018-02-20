using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Processor
{
    public enum DSWriteOption
    {

        /**
         * Do not update data source
         */
        /**
         * Do not update data source
         */
        None,
        /**
         * Update data source synchronously
         */
        WriteThru,
        /**
         * Update data source asynchronously
         */
        WriteBehind,
        /**
         * Update data source synchronously if default provider is configured
         */
        OptionalWriteThru

    }


}
