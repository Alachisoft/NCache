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
using System.Windows.Forms;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// The application
    /// </summary>
    public class Program
    {
        public const string Title = "Customer Directory";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            /// Initialize cache takes a CacheID as argument. Specified CacheID
			/// must be registered. Initializatin won't fail if the cache is not running
			/// and you can start the cache later on while the application is running.
			/// You can use 'NCache Manager Application' or command line tools to 
			/// register and start caches. For more information see NCache help collection.
			/// 
			try
            {
                System.Windows.Forms.Application.Run(new UI.MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception:" + ex.Message, Program.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
