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
using System.Configuration;
using System.Windows.Forms;
using Alachisoft.NCache.Web.Caching;

namespace Alachisoft.NCache.Samples
{
	/// <summary>
	/// 
	/// </summary>
	internal class Program
	{
		public const string Title = "ChatRoom";
        public static Cache _cache;
		
        /// <summary>
		/// 
		/// </summary>
		public Program()
		{
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void  Main()
		{
            string errorString = null;
            string CacheName = ConfigurationManager.AppSettings["CacheID"];
            
            
			/// Uncomment the following line to have NCache throw exceptions when 
			/// an operation fails. This is useful during development, as it will help
			/// understand the cause of the error. By default exceptions are disabled.
			/// If the exceptions are disabled operations will fail silently.
            ///
			/// Initialize cache takes a cache-id as argument. Specified cache-id
			/// must be registered. Initializatin won't fail if the cache is not running
			/// and you can start the cache later on while the application is running.
			/// You can use 'NCache Manager Application' or command line tools to 
			/// register and start caches. For more information see NCache help collection.
			///

			try
            {
                _cache = NCache.Web.Caching.NCache.InitializeCache(CacheName);
                _cache.ExceptionsEnabled = true;
                System.Windows.Forms.Application.Run(new Alachisoft.NCache.Samples.CustomEvents.UI.SigninForm());
                _cache.Dispose();
          	}            
            catch (Exception ex)
            {
                MessageBox.Show("Error occured while trying to initialize Cache named: [" + CacheName +
                                "]\nException: " + ex.Message,
                    Program.Title,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);   
            }
		}
	}
}
