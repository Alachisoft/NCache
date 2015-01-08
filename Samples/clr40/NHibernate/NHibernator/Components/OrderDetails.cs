using System;
using System.Collections.Generic;
using System.Text;

namespace nhibernator.BLL
{
    public class OrderDetails
    {
        private int _productId;
        private int _orderId;

        public virtual int ProductId
        {
            get { return _productId; }
            set { _productId = value; }
        }

        public virtual int OrderId
        {
            get { return _orderId; }
            set { _orderId = value; }
        }

        public override bool Equals(object obj)
        {
            return true;
        }

        public override int GetHashCode()
        {
            return _productId.GetHashCode();
        }

        public override string ToString()
        {
            return _orderId.ToString() + "$" + _productId.ToString();
        }
    }
}
