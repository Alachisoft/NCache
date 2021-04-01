/// This code is taken from 
/// http://www.codeproject.com/dotnet/nhibernatept1.asp

using System;
using NHibernate;
using NHibernate.Cfg;
using System.Collections;
using nhibernator.BLL;
//using NHibernate.Expression;
using Iesi.Collections;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace nhibernator.DLL
{
	/// <summary>
	/// Data Layer Logic for loading/saving Customers
	/// </summary>
	public class CustomerFactory :  IDisposable
	{
		Configuration config;
		ISessionFactory factory;
		ISession session;
        
		/// <summary>
		/// Get All Customers, should rarely be used...
		/// </summary>
		/// <returns>Complete list of customers</returns>
		public IList GetCustomers()
		{
            IList customers = new System.Collections.ArrayList();
			ITransaction tx = null;
			string id = string.Empty;
            try
            {
                if (!session.IsConnected)
                {
                    session.Reconnect();
                }
                tx = session.BeginTransaction();

                IQuery qry = session.CreateQuery("from Customer c")
                                    .SetCacheable(true);
                customers = qry.List();
                tx.Commit();
            }
            catch (Exception ex)
            {

                tx.Rollback();
                session.Clear();
                session.Disconnect();
                throw ex;
            }
			return customers;
		}
		
		/// <summary>
		/// Gets a Customer
		/// </summary>
		/// <param name="CustomerID">string representing customer id</param>
		/// <returns>Object representing customer of "Customer" type. </returns>
		public Customer GetCustomer(string CustomerID)
		{
			Customer customer = null;
			ITransaction tx = null;
			
			//int ordercount =0;
            try
            {
                if (!session.IsConnected)
                {
                    session.Reconnect();
                }
                tx = session.BeginTransaction();

                customer = (Customer)session.Get(typeof(Customer), CustomerID);

                tx.Commit();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                session.Clear();
                session.Disconnect();
                throw ex;
            }
			return customer;
		}

        public Customer GetCustomerOrders(string CustomerID)
        {
            Customer customer = null;
            ITransaction tx = null;

            try
            {
                if (!session.IsConnected)
                {
                    session.Reconnect();
                }
                tx = session.BeginTransaction();

                customer = (Customer)session.Get(typeof(Customer), CustomerID);

                tx.Commit();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                throw ex;
            }
            return customer;
        }

		/// <summary>
		/// Add current customer in database.
		/// </summary>
		/// <param name="cust">Customer to save.</param>
		public void SaveCustomer(Customer customer)
		{
			ITransaction tx = null;
			try
			{
				if(!session.IsConnected)
				{
					session.Reconnect();
				}

				tx = session.BeginTransaction();

                session.Save(customer);
                session.Flush();
                tx.Commit();
                Console.WriteLine("\nCustomer with ID: " + customer.CustomerID + " successfully added into database");
			}
			catch (Exception ex)
			{
				tx.Rollback();
                session.Clear();
				session.Disconnect();
                throw ex.InnerException;
			}
		}

        /// <summary>
        /// Insert/Update current customer in database.
        /// </summary>
        /// <param name="cust">Customer to update.</param>
        public void UpdateCustomer(Customer customer)
        {
            ITransaction tx = null;
            try
            {
                if (!session.IsConnected)
                {
                    session.Reconnect();
                }

                tx = session.BeginTransaction();

                session.Merge(customer);
                session.Flush();
                tx.Commit();
                Console.WriteLine("\nCustomer with ID: " + customer.CustomerID + " successfully updated into database");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                session.Clear();
                session.Disconnect();
                throw ex.InnerException;
                // handle exception
            }
        }

        /// <summary>
        /// Removes the customer with customerID
        /// </summary>
        /// <param name="CustomerID"></param>
        public void RemoveCustomer(string  CustomerID)
        {
            ITransaction tx = null;
            Customer customer;
            try
            {
                if (!session.IsConnected)
                {
                    session.Reconnect();
                }

                tx = session.BeginTransaction();
                IEnumerator enumerator = session.CreateQuery("select cust " + "from Customer cust where " +
                                                            "cust.CustomerID = '" + CustomerID + "'").Enumerable().GetEnumerator();

                enumerator.MoveNext();
                customer = (Customer)enumerator.Current;
                if (customer != null)
                {
                    session.Delete(customer);
                    factory.Evict(typeof(Customer), CustomerID);
                    factory.EvictCollection(typeof(Customer).ToString() + ".Orders", CustomerID);

                    Console.WriteLine("\nCustomer with ID: " + CustomerID + " successfully deleted from database");
                }
                else
                {
                    Console.WriteLine("No such customer exist.");
                }
                tx.Commit();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                session.Clear();
                session.Disconnect();
                throw ex;
                // handle exception
            }
        }

        /// <summary>
        /// Clears and disconnects the session
        /// </summary>
        public void SessionDisconnect()
        {
            session.Clear();
            session.Disconnect();
        }
		
		/// <summary>
		/// Create a customer factory based on the configuration given in the configuration file
		/// </summary>
		public CustomerFactory()
		{
            this.ConfigureLog4Net();
			config = new Configuration();
			config.AddAssembly("nhibernator");
			factory = config.BuildSessionFactory();
			session = factory.OpenSession();

		}

        /// <summary>
        /// Reads Log4Net configurations from application configuration file
        /// </summary>
        private void ConfigureLog4Net()
        {
            log4net.Config.XmlConfigurator.Configure();
        }
	
		/// <summary>
		/// Make sure we clean up session etc.
		/// </summary>
		public void Dispose()
		{
			session.Dispose();
			factory.Close();
		}
	}
}
