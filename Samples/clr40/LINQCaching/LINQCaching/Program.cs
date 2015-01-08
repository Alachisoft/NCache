using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Linq;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Linq;
using System.Configuration;
using Alachisoft.NCache.Sample.Data;

namespace NCacheLINQ
{
	class Program
	{
        private static Datasource _db = new Datasource();
        private static Cache _cache;
        private static string _error = "Some error has occured while executing the query" +
                                       "\nPossible reason might be that the query indexes are not defined" +
                                       "\nFor help see readme!.txt given with this sample";

		static void Main(string[] args)
		{
            Console.WindowWidth = 100;
            string select;
            if (!LoadCache())
            {
                Console.ReadLine();
                return;
            }
            Console.WriteLine("Cache Loaded !");

            IQueryable<Product> products = new NCacheQuery<Product>(_cache);
            
            do
            {
                Console.WriteLine("\n\n1> from product in products where product.ProductID > 10 select product;");
                Console.WriteLine("2> from product in products where product.Category == 4 select product;");
                Console.WriteLine("3> from product in products where product.ProductID < 10 && product.Supplier == 1 select product;");
                Console.WriteLine("x> Exit");
                Console.Write("?> ");
                select = Console.ReadLine();

                switch (select)
                {
                    case "1":
                        try
                        {
                            var result1 = from product in products
                                     where product.ProductID > 10
                                     select product;
                            if (result1 != null)
                            {
                                PrintHeader();
                                foreach (Product p in result1)
                                {
                                    Console.WriteLine("ProductID : " + p.ProductID);
                                }
                            }
                            else
                            {
                                Console.WriteLine("No record found.");
                            }
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
                            var result2 = from product in products
                                     where product.Category == 4
                                     select product;
                            if (result2 != null)
                            {
                                PrintHeader();
                                foreach (Product p in result2)
                                {
                                    Console.WriteLine("ProductID : " + p.ProductID);
                                }
                            }
                            else
                            {
                                Console.WriteLine("No record found.");
                            }
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
                            var result3 = from product in products
                                     where product.ProductID < 10
                                     && product.Supplier == 1
                                     select product;
                            if (result3 != null)
                            {
                                PrintHeader();
                                foreach (Product p in result3)
                                {
                                    Console.WriteLine("ProductID : " + p.ProductID);
                                }
                            }
                            else
                            {
                                Console.WriteLine("No record found.");
                            }
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

        private static bool InitializeCache()
        {
            string cacheName = ConfigurationSettings.AppSettings["CacheName"].ToString();
            try
            {
                _cache = NCache.InitializeCache(cacheName);
                _cache.Clear();
                return true;
            }
            catch(Exception e)
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
                    IDictionaryEnumerator ide = keyVals.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        _cache.Add(ide.Key.ToString(), (Product)ide.Value);
                    }
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception : " + ex.Message);
                return false;
            }
            return true;
        }

        private static void PrintHeader()
        {
            Console.WriteLine();
            Console.WriteLine("Cache contains following records.");
            Console.WriteLine("---------------------------------");
            Console.WriteLine();
        }
	}
}
