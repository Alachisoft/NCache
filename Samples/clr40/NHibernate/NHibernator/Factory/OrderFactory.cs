//===============================================================================
// Copyright© 2014 Alachisoft.  All rights reserved.
//===============================================================================

using System;
using NHibernate;
using NHibernate.Cfg;
using System.Collections;
using nhibernator.BLL;


namespace nhibernator.DLL
{
	/// <summary>
	/// Summary description for OrderDataFactory.
	/// </summary>
	public class OrderFactory : IDisposable
	{
		Configuration config;
		ISessionFactory factory;
		
		public IList GetOrders(string CustomerID)
		{
			IList orders = null;
			ITransaction tx = null;
			ISession session = null;
			try
			{
                
				session = factory.OpenSession();
				tx = session.BeginTransaction();
                IQuery qry = session.CreateQuery("from nhibernator.BLL.Order as order where CustomerID is" + CustomerID);
                orders = qry.List();
				session.Close();
			}
			catch (Exception ex)
			{
				
				Console.Write(ex);
				tx.Rollback();
				session.Close();
				// handle exception. 
			}
			return orders;
		}

		
		public OrderFactory(string cacheId)
		{
			log4net.Config.DOMConfigurator.Configure();
			config = new Configuration();
			config.Configure();
			config.AddAssembly("nhibernator");
			
			factory = config.BuildSessionFactory();
			ISession session = factory.OpenSession();
			ITransaction transaction = session.BeginTransaction();

		}

		/// <summary>
		/// Make sure we clean up session etc.
		/// </summary>
		public void Dispose()
		{
			factory.Close();
		}
	}
}
