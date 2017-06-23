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
using System.Collections;
using System.Configuration;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Sample.Data;

namespace NCacheQuerySample
{
	/// <summary>
    /// Summary description for ObjectQueryLanguage.
	/// </summary>
	class ObjectQueryLanguage
	{
		private static ICollection _keys = new ArrayList();
		private static Datasource _db = new Datasource();
		private static Cache  _cache;
        private static string _error = "Some error has occured while executing the query" +
                                       "\nPossible reason might be that the query indexes are not defined" +
                                       "\nFor help see readme!.txt given with this sample";

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			string select;
            Hashtable values = new Hashtable();
            Console.WindowWidth = 100;

            if (!LoadCache())
            {
                Console.ReadLine();
                return;
            }
			
			Console.WriteLine("Cache loaded !");
			
			do
			{
				Console.WriteLine("\n\n1> select Alachisoft.NCache.Sample.Data.Product where this.ProductID > 10");
                Console.WriteLine("2> select Alachisoft.NCache.Sample.Data.Product where this.Category = 4");
                Console.WriteLine("3> select Alachisoft.NCache.Sample.Data..Product where this.ProductID < 10 and this.Supplier = 1");
				Console.WriteLine("x> Exit");
				Console.Write("?> ");
				select = Console.ReadLine();
				
                switch (select)
				{
					case "1":
                        try
                        {
                            values.Clear();
                            values.Add("ProductID", 10);
                            _keys = _cache.Search("select Alachisoft.NCache.Sample.Data.Product where this.ProductID > ?", values);
                            if (_keys.Count > 0)
                            {
                                PrintHeader();

                                IEnumerator ie = _keys.GetEnumerator();
                                while (ie.MoveNext())
                                {
                                    Console.WriteLine("ProductID : " + ie.Current);
                                }
                            }
                            else
                                Console.WriteLine("No record found.");
                        }
                        catch (Exception)
                        {
                            Console.WriteLine(_error);
                        }
                        Console.WriteLine("Press Enter to continue..");
                        Console.ReadLine();
						break;

					case "2":
                        try
                        {
                            values.Clear();
                            values.Add("Category", 4);
                            _keys = _cache.Search("select Alachisoft.NCache.Sample.Data.Product where this.Category = ?", values);
                            if (_keys.Count > 0)
                            {
                                PrintHeader();

                                IEnumerator ie = _keys.GetEnumerator();
                                while (ie.MoveNext())
                                {
                                    Console.WriteLine("ProductID : " + ie.Current);
                                }
                            }
                            else
                                Console.WriteLine("No record found.");
                        }
                        catch (Exception)
                        {
                            Console.WriteLine(_error);
                        }

                        Console.WriteLine("Press Enter to continue..");
                        Console.ReadLine();
						break;

					case "3":
                        try
                        {
                            values.Clear();
                            values.Add("ProductID", 10);
                            values.Add("Supplier", 1);
                            _keys = _cache.Search("select Alachisoft.NCache.Sample.Data.Product where this.ProductID < ? and this.Supplier = ?", values);
                            if (_keys.Count > 0)
                            {
                                PrintHeader();

                                IEnumerator ie = _keys.GetEnumerator();
                                while (ie.MoveNext())
                                {
                                    Console.WriteLine("ProductID : " + ie.Current);
                                }
                            }
                            else
                                Console.WriteLine("No record found.");
                        }
                        catch (Exception)
                        {
                            Console.WriteLine(_error);
                        }

                        Console.WriteLine("Press Enter to continue..");
                        Console.ReadLine();
						break;

					case "x":
						break;
				}
			} while (select != "x");
            _cache.Dispose();
            Environment.Exit(0);
		}

		private static void PrintHeader()
		{
			Console.WriteLine();
			Console.WriteLine("Cache contains following records.");
			Console.WriteLine("---------------------------------");
			Console.WriteLine();
		}

        private static bool InitializeCache()
        {
            string cacheName = ConfigurationSettings.AppSettings.Get("CacheName").ToString();

            try
            {
                _cache = NCache.InitializeCache(cacheName);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured while trying to initialize Cache named: [" + cacheName + "] \n"
                                    + "Exception: " + e.Message);
                return false;
            }
        }

		private static bool LoadCache()
		{
			try
			{
                if (InitializeCache())
                {
                    Console.WriteLine("Loading products into " + _cache.ToString() + "...");

                    Hashtable keyVals = _db.LoadProducts();

                    String[] keys = new String[keyVals.Keys.Count];
                    CacheItem[] items = new CacheItem[keyVals.Keys.Count];

                    IDictionaryEnumerator ide = keyVals.GetEnumerator();
                    int i = 0;
                    while (ide.MoveNext())
                    {
                        keys[i] = ide.Key.ToString();
                        items[i++] = new CacheItem(ide.Value);
                    }

                    _cache.AddBulk(keys, items);
                }
                else
                    return false;
			}
			catch(Exception ex)
			{
				Console.WriteLine("Exception : " + ex.Message);
				return false;
			}
			return true;
		}
	}
}
