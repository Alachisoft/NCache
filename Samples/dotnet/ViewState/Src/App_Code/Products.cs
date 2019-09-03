// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// NCache ViewState sample
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System;

[Serializable]
public class Product
{
    private int _ProductID;
    private string _ProductName;
    private int _Supplier;
    private string _Category;
    private int _UnitsAvailable;

    /// <summary>
    /// Get/Set the ProductID.
    /// </summary>
    public int ProductID
    {
        get { return _ProductID; }
        set { _ProductID = value; }
    }

    /// <summary>
    /// Get/Set the ContactName.
    /// </summary>
    public int Supplier
    {
        get { return _Supplier; }
        set { _Supplier = value; }
    }

    /// <summary>
    /// Get/Set the CompanyName.
    /// </summary>
    public string ProductName
    {
        get { return _ProductName; }
        set { _ProductName = value; }
    }

    /// <summary>
    /// Get/Set the Category.
    /// </summary>
    public string Category
    {
        get { return _Category; }
        set { _Category = value; }
    }

    /// <summary>
    /// Get/Set the UnitsAvailable.
    /// </summary>
    public int UnitsAvailable
    {
        get { return _UnitsAvailable; }
        set { _UnitsAvailable = value; }
    }

    public override string ToString()
    {
        return "[Product: " +
            " ProductID = " + ProductID +
            " ProductName = " + ProductName +
            " Supplier = " + Supplier +
            " Category = " + Category +
            " UnitsAvailable = " + UnitsAvailable;
    }
}
