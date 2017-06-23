/// This code is taken from 
/// http://www.codeproject.com/dotnet/nhibernatept1.asp
/// Addressing version is NHibernate 1.2.0.GA

using System;
using System.Collections;

namespace nhibernator.BLL
{
	/// <summary>
	/// Summary description for Customer.
	/// </summary>
	/// 
	[Serializable]
	public class Customer// : ICompactSerializable
	{
		#region Private Internal Members
		
		private string m_CustomerID,m_CompanyName,m_ContactName,m_Address,m_City,m_Region,m_PostalCode,m_Country;
		private ICollection m_Orders;
		#endregion
		
		#region Public Properties

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


        public virtual string CompanyName
		{
			get
			{
				return m_CompanyName;
			}
			set
			{
				m_CompanyName = value;
			}
		}


        public virtual string ContactName
		{
			get
			{
				return m_ContactName;
			}
			set
			{
				m_ContactName = value;
			}
		}


        public virtual string Address
		{
			get
			{
				return m_Address;
			}
			set
			{
				m_Address = value;
			}
		}


        public virtual string City
		{
			get
			{
				return m_City;
			}
			set
			{
				m_City = value;
			}
		}


        public virtual string Region
		{
			get
			{
				return m_Region;
			}
			set
			{
				m_Region = value;
			}
		}


        public virtual string PostalCode
		{
			get
			{
				return m_PostalCode;
			}
			set
			{
				m_PostalCode = value;
			}
		}


        public virtual string Country
		{
			get
			{
				return m_Country;
			}
			set
			{
				m_Country = value;
			}
		}

        public virtual ICollection Orders
		{
			get
			{
				return m_Orders;
			}
			set
			{
				m_Orders = value;
			}
		}
		#endregion
	
    }
}
