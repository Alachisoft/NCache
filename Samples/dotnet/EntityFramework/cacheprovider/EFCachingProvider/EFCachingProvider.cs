// ===============================================================================
// Alachisoft (R) NCache Sample Code
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
using System.Linq.Expressions;
using System.Text;
using System.Collections;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// Class that provides the functionality of the sample
    /// </summary>
    class EFCachingProvider
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Run()
        {
            // Initialize customer factory
            NorthwindEntities database = new NorthwindEntities();

            string customerId = "HANIH";
                
            //Add Customer
            AddCustomerToDatabase(customerId, database);

            // Show customer details
            GetCustomerFromDatabase(database);

            // Show Cusotmer's Orders
            GetCustomerOrders(database);

            // Delete Cusotmer
            RemoveCustomer(customerId, ref database);
        }

        /// <summary>
        /// This method adds new customer in to database
        /// </summary>
        /// <param name="customerID"> CustomerID to be added in database </param>
        /// <param name="databaseContext"> Instance of database context </param>
        private static void AddCustomerToDatabase(string customerID, NorthwindEntities databaseContext)
        {
            databaseContext.Customers.Add(new Customer { CustomerID = customerID, ContactName = "Hanih Moos", ContactTitle = "Sales Representative", CompanyName = "Blauer See Delikatessen" });
            databaseContext.SaveChanges();

            Console.WriteLine("Customer is added in database.");
        }

        /// <summary>
        /// Get Customer from database
        /// </summary>
        /// <param name="customerID"> CustomerID to be fetched from database </param>
        /// <param name="databaseContext"> Instance of database context </param>
        private static void GetCustomerFromDatabase(NorthwindEntities databaseContext)
        {
            var customerQuery = from customerDetail in databaseContext.Customers
                                where customerDetail.CustomerID == "TOMSP"
                                select customerDetail;
            PrintCustomerDetail(customerQuery);
        }

        /// <summary>
        /// Get Customer's orders from database
        /// </summary>
        /// <param name="customerID"> CustomerID to be fetched from database </param>
        /// <param name="databaseContext"> Instance of database context </param>
        private static void GetCustomerOrders(NorthwindEntities databaseContext)
        {
            var orderQuery = from customerOrder in databaseContext.Orders
                             where customerOrder.Customer.CustomerID == "TOMSP"
                             select customerOrder;
            PrintCustomerOrders(orderQuery, "TOMSP");
        }

        /// <summary>
        /// Display customer list
        /// </summary>
        /// <param name="list"></param>
        private static void PrintCustomerList(IQueryable<Customer> list)
        {
            Console.WriteLine("\nCustomer ID    Customer Name");
            Console.WriteLine("-----------    -------------");

            if (list != null)
            {
                foreach (Customer customer in list)
                {
                    Console.WriteLine("{0,-13}  {1,-30}", customer.CustomerID, customer.ContactName);
                }
            }
        }

        /// <summary>
        /// Display customer details
        /// </summary>
        /// <param name="customer"></param>
        private static void PrintCustomerDetail(IQueryable<Customer> list)
        {
            foreach (Customer customer in list)
            {
                if (customer != null)
                {

                    Console.WriteLine("\nCustomer's Detail");
                    Console.WriteLine("-----------------");

                    Console.WriteLine("Customer ID : " + customer.CustomerID);
                    Console.WriteLine("Name        : " + customer.ContactName);
                    Console.WriteLine("Company     : " + customer.CompanyName);
                    Console.WriteLine("Address     : " + customer.Address);
                }
            }
        }

        /// <summary>
        /// Display customer orders
        /// </summary>
        /// <param name="customer"></param>
        private static void PrintCustomerOrders(IEnumerable<Order> list, string id)
        {
            IEnumerator ie = list.GetEnumerator();
            ie.MoveNext();

            Order selectedOrder = (Order)ie.Current;
            if (selectedOrder != null)
            {
                if (selectedOrder.Customer != null)
                {
                    Console.WriteLine("\n" + selectedOrder.Customer.ContactName + "'s Orders");
                }
                else
                {
                    NorthwindEntities database = new NorthwindEntities();

                    IQueryable<Customer> customerQuery = database.Customers.AsQueryable<Customer>();
                    customerQuery = customerQuery.Where(customer => customer.CustomerID == id);

                    IEnumerator enumerator = customerQuery.GetEnumerator();
                    enumerator.MoveNext();

                    Customer cust = (Customer)enumerator.Current;
                    Console.WriteLine("\n" + cust.CustomerID + "'s Orders");
                }
                Console.WriteLine("------------------------");
                Console.WriteLine(" {0,-10} {1,-25} {2,-20}", "Order ID", "Order Date", "Ship Name");
                Console.WriteLine(" {0,-10} {1,-25} {2,-20}", "--------", "-----------", "---------");
                
                PrintOrderDetail(selectedOrder);
            }
            while (ie.MoveNext())
            {
                if (ie.Current != null)
                {
                    PrintOrderDetail((Order)ie.Current);
                }
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
        /// Remove Customers and all tables dependant including Orders and Order details
        /// </summary>
        /// <param name="order"></param>
        private static void RemoveCustomer(string id, ref NorthwindEntities database)
        {
            IQueryable<Customer> customerQuery = database.Customers.AsQueryable<Customer>();
            IQueryable<Order> orderQuery = database.Orders.AsQueryable<Order>();
            IQueryable<Order_Detail> orderDetailQuery = database.Order_Details.AsQueryable<Order_Detail>();

            customerQuery = from customertoDelete in database.Customers
                            where customertoDelete.CustomerID == id
                            select customertoDelete;

            orderQuery = from customerOrder in database.Orders
                         where customerOrder.Customer.CustomerID == id
                         select customerOrder;

            foreach (Order order in orderQuery)
            {

                orderDetailQuery = from detail in database.Order_Details
                                   where detail.Order.OrderID == order.OrderID
                                   select detail;

                foreach (Order_Detail orderDetails in orderDetailQuery)
                {
                    database.Order_Details.Attach(orderDetails);
                    database.Order_Details.Remove(orderDetails);

                }
            }
            database.SaveChanges();

            foreach (Order orders in orderQuery)
            {
                database.Orders.Attach(orders);
                database.Orders.Remove(orders);
            }
            database.SaveChanges();

            List<Customer> customer = new List<Customer>();
            foreach (Customer c in customerQuery)
                customer.Add(c);
            if (customer.Count() != 0)
            {
                database.Customers.Attach(customer[0]);
                database.Customers.Remove(customer[0]);
                database.SaveChanges();
            }
        }
    }
}

