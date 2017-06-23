// ===============================================================================
// Alachisoft (R) NCache Sample Code
// NCache Product Class used by samples
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System;

namespace Alachisoft.NCache.Sample.Data
{

    [Serializable]
    public class Product
    {
        private int _productId;
        private string _name;
        private string _productClass;
        private int _category;
        private int _supplier;
        private int _unitsAvailable;

        public Product()
        { }

        public Product(int productId, string name, string productClass, int category)
        {
            this._productId = productId;
            this._name = name;
            this._productClass = productClass;
            this._category = category;
        }

        public virtual int ProductID
        {
            set { this._productId = value; }
            get { return this._productId; }
        }


        public virtual string Name
        {
            set { this._name = value; }
            get { return this._name; }
        }


        public virtual string ClassName
        {
            set { this._productClass = value; }
            get { return this._productClass; }
        }


        public virtual int Category
        {
            set { this._category = value; }
            get { return this._category; }
        }

        public int Supplier
        {
            get { return _supplier; }
            set { _supplier = value; }
        }

        public int UnitsAvailable
        {
            get { return _unitsAvailable; }
            set { _unitsAvailable = value; }
        }
    }

}