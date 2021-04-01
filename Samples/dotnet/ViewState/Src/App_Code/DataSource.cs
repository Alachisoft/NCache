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
using System.Data;

/// <summary>
/// Summary description for DataSource
/// </summary>
[Serializable]
public class DataSource
{
    //--- List of Products available
    private String[] _ProductNameList = {
        "Chai", "Chang", "Aniseed Syrup", "Chef Anton's Cajun Seasoning", "Chef Anton's Gumbo Mix", "Grandma's Boysenberry Spread",
        "Uncle Bob's Organic Dried Pears", "Northwoods Cranberry Sauce", "Mishi Kobe Niku", "Ikura", "Queso Cabrales", "Queso Manchego La Pastora",
        "Konbu", "Tofu", "Genen Shouyu", "Pavlova", "Alice Mutton", "Carnarvon Tigers", "Teatime Chocolate Biscuits", "Sir Rodney's Marmalade",
        "Sir Rodney's Scones", "Gustaf's Knäckebröd", "Tunnbröd", "Guaraná Fantástica", "NuNuCa Nuß-Nougat-Creme", "Gumbär Gummibärchen",
        "Schoggi Schokolade", "Rössle Sauerkraut", "Thüringer Rostbratwurst", "Nord-Ost Matjeshering", "Gorgonzola Telino", "Mascarpone Fabioli",
        "Geitost", "Sasquatch Ale", "Steeleye Stout", "Inlagd Sill", "Genen SHOuyu", /*"Gravad lax",*/ "Côte de Blaye", "Chartreuse verte",
        "Boston Crab Meat", "Ipoh Coffee", "Gula Malacca", "Røgede sild", "Spegesild", "Zaanse koeken", "Chocolade", "Maxilaku", "Valkoinen suklaa",
        "Filo Mix", "Perth Pasties", "Tourtière", "Pâté chinois", "Ravioli Angelo", "Gudbrandsdalsost", "Outback Lager", "Fløtemysost", "Röd Kaviar", "Longlife Tofu", "Lakkalikööri"
    };

    private String[] _productCategory = { "General", "NCache", "TierDeveloper", "Alachisoft", "Diyatech", "NWebCache", "WebServiceAccelerator" };
    private String[] EmployeeFirstName = { "Nancy", "Andrew", "Janet", "Margaret", "Steven", "Michael", "Robert", "Laura", "Anne", "sada", "sada", "Nancy", "Malta" };
    private String[] EmployeeLastName = { "Buchanan", "Callahan", "Davolio", "Davolio", "Dodsworth", "eqeq", "eqeq", "Fuller", "Jalta", "King", "Leverling", "Peacock", "Suyama" };

    public Product[] LoadProducts(long TotalItems)
    {
        try
        {
            Product[] products = new Product[TotalItems];
            String[] ProductList = new String[TotalItems];

            if (TotalItems > _ProductNameList.Length)
            {
                for (int k = _ProductNameList.Length; k < TotalItems; k++)
                    ProductList[k] = "Kaka.Mana-" + k.ToString();

                _ProductNameList.CopyTo(ProductList, 0);
            }

            int productID = 0;
            Random rand = new Random();
            while (productID < ProductList.Length)
            {
                Product product = new Product();
                product.ProductID = productID + 1;
                if (TotalItems > _ProductNameList.Length)
                    product.ProductName = ProductList[productID];
                else
                    product.ProductName = _ProductNameList[productID];
                product.Supplier = rand.Next(1, 10);
                product.Category = _productCategory[rand.Next(0, 7)];
                product.UnitsAvailable = rand.Next(1, 1000);

                products[productID] = product;
                productID++;
            }

            return products;
        }
        catch (Exception e)
        {
            System.Diagnostics.Trace.WriteLine(e);
            return null;
        }
    }

    public Product[] LoadSameProducts(long TotalItems)
    {
        try
        {
            Product[] products = new Product[TotalItems];
            String[] ProductList = new String[TotalItems];

            if (TotalItems > _ProductNameList.Length)
            {
                for (int k = _ProductNameList.Length; k < TotalItems; k++)
                    ProductList[k] = "Kaka.Mana-" + k.ToString();

                _ProductNameList.CopyTo(ProductList, 0);
            }

            int productID = 0;
            int categoryID = 0;
            while (productID < ProductList.Length)
            {
                Product product = new Product();
                product.ProductID = productID + 1;
                if (TotalItems > _ProductNameList.Length)
                    product.ProductName = ProductList[productID];
                else
                    product.ProductName = _ProductNameList[productID];
                product.Supplier = productID;
                product.Category = _productCategory[categoryID];
                product.UnitsAvailable = 500;

                products[productID] = product;
                productID++;
                categoryID++;
                if (categoryID >= _productCategory.Length) categoryID = 0;
            }

            return products;
        }
        catch (Exception e)
        {
            System.Diagnostics.Trace.WriteLine(e);
            return null;
        }
    }

    public DataTable LoadProduct(int indexproductID)
    {
        DataTable ProductList = new DataTable();
        try
        {
            DataTable table = new DataTable();
            table.Columns.Add("ProductID", typeof(int));
            table.Columns.Add("ProductName", typeof(string));
            table.Columns.Add("Supplier", typeof(int));
            table.Columns.Add("Category", typeof(string));
            table.Columns.Add("UnitsAvailable", typeof(int));

            //
            // Here we add five DataRows.
            //
            table.Rows.Add(indexproductID, _ProductNameList[indexproductID], 3, _productCategory[indexproductID], 3);

            return table;
        }

        catch (Exception e)
        {
            System.Diagnostics.Trace.WriteLine(e);
            return null;
        }
    }
}
