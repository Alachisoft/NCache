// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System;
using System.Data;
using System.Data.SQLite;
using System.Data.Common;
using System.Globalization;
using Alachisoft.NCache.Sample.Data;

namespace BackingSource.Utility
{
    /// <summary>
    /// Class that helps read and write customer information to an xml
    /// datasource.
    /// </summary>
    internal class SqliteDataSource
    {
        private DbConnection _dbConnection;

        /// <summary>
        /// Establish connection with the datasource.
        /// </summary>
        /// <param name="connString"></param>
        public void Connect(string connString)
        {
            _dbConnection = new SQLiteConnection();
            _dbConnection.ConnectionString = connString;
            _dbConnection.Open();
        }

        /// <summary>
        /// Releases the connection.
        /// </summary>
        public void DisConnect()
        {
            if (_dbConnection != null)
                _dbConnection.Close();
        }

        /// <summary>
        /// Loads a <see cref="Customer"/> object from the datasource.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object LoadCustomer(Object key)
        {
            object[] customerId = { key };
            IDataReader reader = null;

            DbCommand cmd = _dbConnection.CreateCommand();
            cmd.CommandText = String.Format(CultureInfo.InvariantCulture,
                                "Select CustomerID, ContactName, CompanyName, Address," +
                                "City, Country, PostalCode, Phone, Fax" +
                                " From Customers where CustomerID = '{0}'",
                                customerId);
            reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                Customer objCustomer = new Customer();

                objCustomer.CustomerID = reader["CustomerID"].ToString();
                objCustomer.ContactName = reader["ContactName"].ToString();
                objCustomer.CompanyName = reader["CompanyName"].ToString();
                objCustomer.Address = reader["Address"].ToString();
                objCustomer.City = reader["City"].ToString();
                objCustomer.Country = reader["Country"].ToString();
                objCustomer.PostalCode = reader["PostalCode"].ToString();
                objCustomer.ContactNo = reader["Phone"].ToString();
                objCustomer.Fax = reader["Fax"].ToString();
                reader.Close();
                return objCustomer;
            }
            reader.Close();

            return null;
        }

        /// <summary>
        /// Save <see cref="Customer"/> information to datasource.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool SaveCustomer(Customer val)
        {
            int rowsChanged = 0;
            string[] customer = {val.CustomerID,val.ContactName,val.CompanyName,
                                    val.Address,val.City,val.Country,val.PostalCode,
                                    val.ContactNo,val.Fax};

            DbCommand cmd = _dbConnection.CreateCommand();
            cmd.CommandText = String.Format(CultureInfo.InvariantCulture,
                                            "Update Customers " +
                                            "Set CustomerID='{0}'," +
                                            "ContactName='{1}',CompanyName='{2}'," +
                                            "Address='{3}',City='{4}'," +
                                            "Country='{5}',PostalCode='{6}'," +
                                            "Phone='{7}',Fax='{8}'" +
                                            " Where CustomerID = '{0}'", customer);
            rowsChanged = cmd.ExecuteNonQuery();
            if (rowsChanged > 0)
            {
                return true;
            }
            return false;
        }
    }
}
