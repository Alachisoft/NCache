/// This code is taken from 
/// http://www.codeproject.com/dotnet/nhibernatept1.asp
/// Addressing version is NHibernate 1.2.0.GA

using System;

namespace nhibernator.BLL
{
	/// <summary>
	/// Summary description for Product.
	/// </summary>
	public class Product
	{
		
		#region private internal members
		private int m_OrderID;
		private int m_ProductID;
		private string m_ProductName;
		private decimal m_UnitPrice;
		
		#endregion
		
		#region public properties


        public virtual int ProductID
		{
			get
			{
				return m_ProductID;
			}
			set
			{
				m_ProductID = value;
			}
		}


        public virtual string ProductName
		{
			get
			{
				return m_ProductName;
			}
			set
			{
				m_ProductName = value;
			}

		}


        public virtual decimal UnitPrice
		{
			get
			{
				return m_UnitPrice;
			}
			set
			{
				m_UnitPrice = value;
			}
		}
		#endregion


		public Product()
		{
			//
			// TODO: Add constructor logic here
			//
		}
	}
}
