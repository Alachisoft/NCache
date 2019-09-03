// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// NCache NHibernate sample
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System;
using System.Collections;
using nhibernator.BLL;
using nhibernator.DLL;


namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// This class is used to test the NHibernate.Mapper that is based on NHibernate
    /// To load the data from Customers and Orders table of Northwind database.
    /// </summary>
    /// 
    public class NHibernate
    {
        public static CustomerFactory _customerFactory;

        public static void Run()
        {
            // Initialize customer factory
            _customerFactory = new CustomerFactory();
            Console.WriteLine("NHibernate test started");

            Console.WriteLine("Loading customers into the cache...");

            // Load customers into the cache
            _customerFactory.GetCustomers();
            _customerFactory.SessionDisconnect();
            Console.WriteLine("Customers information loaded in the cache");

            // Prints the customers in the cache
            PrintCustomerList(_customerFactory.GetCustomers());
            
            Random randGen = new Random();
            int randNum = randGen.Next(0, 9);
            string id = GetCustomerId(randNum);

            // Adds a new customer with randomId
            _customerFactory.SaveCustomer(GenerateCustomer(true, id));

            // Prints the customer details
            PrintCustomerDetail(_customerFactory.GetCustomer(id));

            // Fetches the customer Orders
            Customer customer = _customerFactory.GetCustomerOrders(id);
            PrintCustomerOrders(customer);

            // Updates the customer
            Customer tempCust = GenerateCustomer(false, id);
            if (tempCust != null)
            {
                _customerFactory.UpdateCustomer(tempCust);
            }

            // Deletes the customer
            _customerFactory.RemoveCustomer(id);

            // Disconnect session
            _customerFactory.SessionDisconnect();

            // Dispose customer factory once done
            _customerFactory.Dispose();
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
            catch (Exception e)
            {
            }
            Console.WriteLine("Please enter a valid choice (1 - 5)");
            return GetUserChoice();
        }

        /// <summary>
        /// Get user customer id from user
        /// </summary>
        /// <returns></returns>
        private static string GetCustomerId(int random)
        {
            return "CST-"+ random;
        }

        /// <summary>
        /// Display customer list
        /// </summary>
        /// <param name="list"></param>
        private static void PrintCustomerList(IList list)
        {
            Console.WriteLine("Customer ID    Customer Name");
            Console.WriteLine("-----------    -------------");

            if (list != null)
            {
                foreach (Customer customer in list)
                {
                    Console.WriteLine("{0,-13}  {1,-30}", customer.CustomerID, customer.ContactName);
                }
            }
            Console.WriteLine("");
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
            else
            {
                Console.WriteLine("No such customer exist.");
            }
        }

        /// <summary>
        /// Display customer orders
        /// </summary>
        /// <param name="customer"></param>
        private static void PrintCustomerOrders(Customer customer)
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
        private static void PrintOrderDetail(Order order)
        {
            if (order != null)
                Console.WriteLine(" {0,-10} {1,-25} {2,-20}", order.OrderID, order.OrderDate.ToString(), order.ShipName);
        }

        /// <summary>
        /// Generates  new customer with specified customerId
        /// </summary>
        /// <param name="bAddUser">True for adding new record. False in case of update.</param>
        /// <returns></returns>
        private static Customer GenerateCustomer(bool bAddUser, string customerID)
        {
            Customer customer = new Customer();
            customer.CustomerID = customerID;

            if (!bAddUser)
            {
                if (_customerFactory.GetCustomer(customer.CustomerID) == null)
                {
                    Console.WriteLine("No such customer exist.");
                    return null;
                }
            }
            customer.ContactName = "ContactName" + customerID;
            customer.CompanyName = "CompanyName" + customerID;
            customer.Country = "Country" + customerID;
            customer.City = "City" + customerID;
            customer.Address = "Address" + customerID;

            return customer;
        }
    }
}
