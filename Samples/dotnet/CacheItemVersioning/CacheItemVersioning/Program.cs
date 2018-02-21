// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Samples
{
    /// ******************************************************************************
    /// <summary>
    /// A sample program that demonstrates how to utilize the async CRUD api in NCache. The api requires a callback method
    /// which is invoked once the async operation is performed. 
    /// 
    /// Requirements:
    ///     1. A running NCache cache
    ///     2. Connection attributes in app.config
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Alachisoft.NCache.Samples.CacheItemVersioning.Run();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }
    }
}
