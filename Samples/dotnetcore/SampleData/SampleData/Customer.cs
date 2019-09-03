using System;

// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

namespace Alachisoft.NCache.Sample.Data
{
    /// <summary>
    /// Model class for Customers
    /// </summary>
    [Serializable]
    public class Customer
    {
        public Customer()
        { }

        /// <summary>
        /// Unique Id of the customer
        /// </summary>
        public string CustomerID
        {
            get;
            set;
        }

        /// <summary>
        /// Contact name of the customer
        /// </summary>
        public virtual string ContactName 
        {
            set;
            get;
        }

        /// <summary>
        /// Company the customer works for
        /// </summary>
        public virtual string CompanyName
        {
            set;
            get;
        }

        /// <summary>
        /// Contact number of the customer
        /// </summary>
        public virtual string ContactNo
        {
            set;
            get;
        }

        /// <summary>
        /// Residential address of the customer
        /// </summary>
        public virtual string Address
        {
            set;
            get;
        }

        /// <summary>
        /// Residence city of the customer
        /// </summary>
        public virtual string City
        {
            set;
            get;
        }

        /// <summary>
        /// Nationality of the customer
        /// </summary>
        public virtual string Country
        {
            set;
            get;
        }

        /// <summary>
        /// Postal code of the customer
        /// </summary>
        public virtual string PostalCode
        {
            set;
            get;
        }

        /// <summary>
        /// Fax number of the customer
        /// </summary>
        public virtual string Fax
        {
            set;
            get;
        }
    }
}