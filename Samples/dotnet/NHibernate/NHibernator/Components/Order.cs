/// This code is taken from 
/// http://www.codeproject.com/dotnet/nhibernatept1.asp
/// Addressing version is NHibernate 1.2.0.GA

using System;
using System.Collections;
using System.Collections.Generic;

namespace nhibernator.BLL
{
	/// <summary>
	/// Summary description for Order.
	/// </summary>
	public class Order
	{
		#region private internal members
	
		private int m_OrderID;
		private string m_CustomerID;
		private DateTime m_OrderDate;
		private DateTime m_ShippedDate;
		private string m_ShipName,m_ShipAddress,m_ShipCity,m_ShipRegion,m_ShipPostalCode;
		private IEnumerable<Product> m_Products;
		private Customer m_OCustomer;

		#endregion
		
		#region public properties

        public virtual Customer OCustomer
		{
			get
			{
				return m_OCustomer;
			}
			set
			{
				m_OCustomer = value;
			}
		}

        public virtual int OrderID
		{
			get
			{
				return m_OrderID;
			}
			set
			{
				m_OrderID = value;
			}
		}


        public virtual string CustomerID
		{
			get
			{
				return m_CustomerID;
			}
			set
			{
				m_CustomerID = value;
			}
		}


        public virtual DateTime OrderDate
		{
			get
			{
				return m_OrderDate;
			}
			set
			{
				m_OrderDate = value;
			}
		}


        public virtual DateTime ShippedDate
		{
			get
			{
				return m_ShippedDate;
			}
			set
			{
				m_ShippedDate = value;
			}
		}


        public virtual string ShipName
		{
			get
			{
				return m_ShipName;
			}
			set
			{
				m_ShipName = value;
			}
		}


        public virtual string ShipAddress
		{
			get
			{
				return m_ShipAddress;
			}
			set
			{
				m_ShipAddress = value;
			}
		}


        public virtual string ShipCity
		{
			get
			{
				return m_ShipCity;
			}
			set
			{
				m_ShipCity=value;
			}
		}


        public virtual string ShipRegion
		{
			get
			{
				return m_ShipRegion;
			}
			set
			{
				m_ShipRegion = value;
			}
		}


        public virtual string ShipPostalCode
		{
			get
			{
				return m_ShipPostalCode;
			}
			set
			{
				m_ShipPostalCode = value;
			}
		}


        public virtual IEnumerable<Product> Products
		{
			get
			{
				return m_Products;
			}
			set
			{
				m_Products = value;
			}
		}


		#endregion

 
		
		public Order()
		{
			//
			// TODO: Add constructor logic here
			//
		}
	}
}
