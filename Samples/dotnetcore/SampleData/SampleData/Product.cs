using System;
using System.Text;

// ===============================================================================
// Alachisoft (R) NCache Sample Code
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
    /// Model class for products
    /// </summary>
    [Serializable]
    public class Product
    {
        public Product() { }

        /// <summary>
        /// Unique id assigned to each product
        /// </summary>
        public virtual int Id
        {
            set;
            get;
        }

        /// <summary>
        /// Price of one unit of this product
        /// </summary>
        public Decimal UnitPrice
        {
            get;
            set;
        }

        /// <summary>
        /// Name of the product
        /// </summary>
        public virtual string Name
        {
            set;
            get;
        }

        /// <summary>
        /// Class to which the product belongs
        /// </summary>
        public virtual string ClassName
        {
            set;
            get;
        }

        /// <summary>
        /// Category of the product
        /// </summary>
        public virtual string Category
        {
            set;
            get;
        }

        /// <summary>
        /// Quantity per unit of the product
        /// </summary>
        public string QuantityPerUnit
        {
            get;
            set;
        }

        /// <summary>
        /// Unit in stock of the product
        /// </summary>
        public int UnitsAvailable
        {
            get;
            set;
        }

        /// <summary>
        /// This method returns the information about this product in string format.
        /// </summary>
        /// <returns> Returns the information about this product. </returns>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("[");

            builder.Append("Name ");
            builder.Append(Name);

            builder.Append(", Quantity/Unit ");
            builder.Append(QuantityPerUnit);

            builder.Append(", UnitPrice ");
            builder.Append(UnitPrice);

            builder.Append(", UnitsAvailable ");
            builder.Append(UnitsAvailable);

            builder.Append("]");

            return builder.ToString();
        }
    }
}