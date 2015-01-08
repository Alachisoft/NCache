//===============================================================================
// Copyright© 2014 Alachisoft.  All rights reserved.
//===============================================================================

using System;
using System.Collections;
using nhibernator.BLL;
using nhibernator.DLL;


namespace Alachisoft.NCache.Samples.NHibernate
{
	/// <summary>
	/// This class is used to test the NHibernate.Mapper that is based on NHibernate
	/// To load the data from Customers and Orders table of Northwind database.
	/// </summary>
    /// 
	public class NHibernateTest
	{
        public static CustomerFactory cf = null;
        /// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		public static void Main(string[] args)
		{
            try
            {
                // Initialize customer factory
                cf = new CustomerFactory();
                Console.WriteLine("NHibernate test started");

                Console.WriteLine("Loading customers into the cache...");

                cf.GetCustomers();
                cf.SessionDisconnect();
                Console.WriteLine("Customers information loaded in the cache");
                

                int choice = 0;
                while (choice != 7)
                {
                    choice = GetUserChoice();
                    string id;
                    try
                    {
                        switch (choice)
                        {
                            case 1:
                                PrintCustomerList(cf.GetCustomers());
                                cf.SessionDisconnect();
                                break;
                            case 2:
                                id = GetCustomerId();
                                PrintCustomerDetail(cf.GetCustomer(id));
                                cf.SessionDisconnect();
                                break;
                            case 3:
                                id = GetCustomerId();
                                Customer customer = cf.GetCustomerOrders(id);
                                PrintCustomerOrders(customer);
                                cf.SessionDisconnect();
                                break;
                            case 4:
                                id = GetCustomerId();
                                cf.RemoveCustomer(id);
                                cf.SessionDisconnect();
                                break;
                            case 5:
                                cf.SaveCustomer(AddCustomer(true));
                                cf.SessionDisconnect();
                                break;
                            case 6:
                                Customer tempCust = AddCustomer(false);
                                if (tempCust != null)
                                {
                                    cf.UpdateCustomer(tempCust);
                                    cf.SessionDisconnect();
                                }
                                break;
                        }
                    }
                    catch(Exception ex) 
                    {
                        Console.WriteLine("\n" + ex + "\n");
                        Console.Read();
                    }
                }
                cf.Dispose();
            }
                catch (Exception e)
            {
                Console.WriteLine("Error:" + e.Message);
                if (e.InnerException != null)
                    Console.WriteLine(e.InnerException.Message);
                Console.Read();
			}
		}



		/// <summary>
		/// Get the next user option
		/// </summary>
		/// <returns></returns>
        private static int GetUserChoice()
        {
            Console.WriteLine("");
            Console.WriteLine(" 1- View customers list");
            Console.WriteLine(" 2- View customer details");
            Console.WriteLine(" 3- View customer orders");
            Console.WriteLine(" 4- Delete customer");
            Console.WriteLine(" 5- Add customer");
            Console.WriteLine(" 6- Update customer");
            Console.WriteLine(" 7- Exit");
            Console.WriteLine("");

            Console.Write("Enter your choice (1 - 7): ");
            try
            {
                int choice = Convert.ToInt32(Console.ReadLine());
                if (choice >= 1 && choice <= 7)
                    return choice;
            }
            catch (Exception)
            {
            }
            Console.WriteLine("Please enter a valid choice (1 - 5)");
            return GetUserChoice();
        }


		/// <summary>
		/// Get user customer id from user
		/// </summary>
		/// <returns></returns>
		private static string GetCustomerId()
		{
			Console.Write("Enter customer ID: ");
			return Console.ReadLine().ToUpper();
		}


		/// <summary>
		/// Display customer list
		/// </summary>
		/// <param name="list"></param>
		private static void PrintCustomerList(IList list)
		{
			Console.WriteLine("Customer ID    Customer Name" );
			Console.WriteLine("-----------    -------------" );

			if(list != null)
			{
				foreach(Customer customer in list)
				{
					Console.WriteLine("{0,-13}  {1,-30}",customer.CustomerID ,customer.ContactName);
				}
			}
		}

		/// <summary>
		/// Display customer details
		/// </summary>
		/// <param name="customer"></param>
		private static void PrintCustomerDetail(Customer customer)
		{
            if (customer != null)
            {
                Console.WriteLine("Customer's Detail");
                Console.WriteLine("-----------------");

                Console.WriteLine("Customer ID : " + customer.CustomerID);
                Console.WriteLine("Name        : " + customer.ContactName);
                Console.WriteLine("Company     : " + customer.CompanyName);
                Console.WriteLine("Address     : " + customer.Address);
            }
            else {
                Console.WriteLine("No such customer exist.");
            }
		}
		
		/// <summary>
		/// Display customer orders
		/// </summary>
		/// <param name="customer"></param>
		static void PrintCustomerOrders(Customer customer)
		{
            if (customer != null)
            {
                Console.WriteLine(customer.ContactName + "'s Orders");
                Console.WriteLine("------------------------");

                if (customer.Orders != null)
                {
                    Console.WriteLine(" {0,-10} {1,-25} {2,-20}", "Order ID", "Order Date", "Ship Name");
                    Console.WriteLine(" {0,-10} {1,-25} {2,-20}", "--------", "----------e", "---------");
                    foreach (Order order in customer.Orders)
                    {
                        PrintOrderDetail(order);
                    }
                }
            }
            else
            {
                Console.WriteLine("No such customer exist.");
            }
		}
		
		/// <summary>
		/// Display order details
		/// </summary>
		/// <param name="order"></param>
		static void PrintOrderDetail(Order order)
		{
			if(order != null)
                Console.WriteLine(" {0,-10} {1,-25} {2,-20}",order.OrderID,order.OrderDate.ToString(),order.ShipName); 
		}

        /// <summary>
        /// Adds or updates Customer record
        /// </summary>
        /// <param name="bAddUser">True for adding new record. False in case of update.</param>
        /// <returns></returns>
        static Customer AddCustomer(bool bAddUser)
        {
            bool validID = false;
            string userInput = "";
            Customer customer = new Customer();

            while (!validID)
            {
                Console.Write("\nEnter Customer ID (maximum length = 5): ");
                customer.CustomerID = Console.ReadLine().ToUpper();
                if (customer.CustomerID.Length > 5 || customer.CustomerID == "")
                {
                    Console.WriteLine("Exception: CustomerID cannot accept string of length > 5");
                }
                else
                    validID = true;
            }
            if (!bAddUser)
            {
                if (cf.GetCustomer(customer.CustomerID) == null)
                {
                    Console.WriteLine("No such customer exist.");
                    return null;
                }
            }

            Console.Write("\nEnter Customer Name: ");
            userInput = Console.ReadLine();
            customer.ContactName = (userInput == "") ? " " : userInput;

            Console.Write("\nEnter Customer Company Name: ");
            userInput = Console.ReadLine();
            customer.CompanyName = (userInput == "") ? " " : userInput;

            Console.Write("\nEnter Country Name: ");
            userInput = Console.ReadLine();
            customer.Country = (userInput == "") ? " " : userInput;
            
            Console.Write("\nEnter City: ");
            userInput = Console.ReadLine();
            customer.City = (userInput == "") ? " " : userInput;

            Console.Write("\nEnter Address: ");
            userInput = Console.ReadLine();
            customer.Address = (userInput == "") ? " " : userInput;


            return customer;
        }
	}

}
