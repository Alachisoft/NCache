// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// NCache Customer Class used by samples
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
    public class Customer
    {
        public string name;
        public int age;
        public string contactNo;
        public string address;
        public string gender;

        public Customer()
        {

        }

        public virtual string Name
        {
            set
            {
                this.name = value;
            }
            get
            {
                return this.name;
            }
        }


        public virtual int Age
        {
            set
            {
                this.age = value;
            }
            get
            {
                return this.age;
            }
        }


        public virtual string ContactNo
        {
            set
            {
                this.contactNo = value;
            }
            get
            {
                return this.contactNo;
            }
        }


        public virtual string Address
        {
            set
            {
                this.address = value;
            }
            get
            {
                return this.address;
            }
        }


        public virtual string Gender
        {
            set
            {
                this.gender = value;
            }
            get
            {
                return this.gender;
            }
        }


    }

}