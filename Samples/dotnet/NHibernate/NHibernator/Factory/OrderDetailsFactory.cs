/// This code is taken from 
/// http://www.codeproject.com/dotnet/nhibernatept1.asp

using System;
using NHibernate;
using NHibernate.Cfg;
using System.Collections;
using nhibernator.BLL;
using Iesi.Collections;

namespace nhibernator.DLL
{
    /// <summary>
    /// Data Layer Logic for loading/saving Customers
    /// </summary>
    public class OrderDetailsFactory : IDisposable
    {
        Configuration config;
        ISessionFactory factory;
        ISession session;

        /// <summary>
        /// Get All Customers, should rarely be used...
        /// </summary>
        /// <returns>Complete list of customers</returns>
        public IList GetOrderDetails()
        {
            IList orderDetails = new System.Collections.ArrayList();
            ITransaction tx = null;
            string id = string.Empty;
            try
            {
                if (!session.IsConnected)
                {
                    session.Reconnect();
                }
                tx = session.BeginTransaction();

                IQuery qry = session.CreateQuery("from OrderDetails d");
                orderDetails = qry.List();
                tx.Commit();
            }
            catch (Exception ex)
            {

                tx.Rollback();
                session.Clear();
                session.Disconnect();
                throw ex;
            }
            return orderDetails;
        }

        /// <summary>
        /// Create a customer factory based on the configuration given in the configuration file
        /// </summary>
        public OrderDetailsFactory()
        {
            config = new Configuration();
            config.AddAssembly("nhibernator");
            factory = config.BuildSessionFactory();
            session = factory.OpenSession();
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
