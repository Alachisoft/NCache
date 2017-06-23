// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// NCache Events sample
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================


using System;
using System.IO;
using System.Data;
using Microsoft.Win32;
using System.Data.OleDb;
using System.Globalization;
using System.Collections;

namespace Alachisoft.NCache.Sample.Data
{
	/// <summary>
	/// Class that helps to load products information.
	/// </summary>
	public class Datasource
	{
		private Hashtable _keyVals;

        //List of Products available
        private String[] _ProductName = { "Chai", "Chang", "Aniseed Syrup", "Chef Anton's Cajun Seasoning", 
                                          "Chef Anton's Gumbo Mix", "Grandma's Boysenberry Spread", 
                                          "Uncle Bob's Organic Dried Pears", "Northwoods Cranberry Sauce", 
                                          "Mishi Kobe Niku", "Ikura", "Queso Cabrales", "Queso Manchego La Pastora",
                                          "Konbu", "Tofu", "Genen Shouyu", "Pavlova", "Alice Mutton", 
                                          "Carnarvon Tigers", "Teatime Chocolate Biscuits", "Sir Rodney's Marmalade",
                                          "Sir Rodney's Scones", "Gustaf's Knäckebröd", "Tunnbröd",
                                          "Guaraná Fantástica", "NuNuCa Nuß-Nougat-Creme", "Gumbär Gummibärchen",
                                          "Schoggi Schokolade", "Rössle Sauerkraut", "Thüringer Rostbratwurst",
                                          "Nord-Ost Matjeshering", "Gorgonzola Telino", "Mascarpone Fabioli",
                                          "Geitost", "Sasquatch Ale", "Steeleye Stout", "Inlagd Sill", "Gravad lax",
                                          "Côte de Blaye", "Chartreuse verte", "Boston Crab Meat", "Ipoh Coffee",
                                          "Gula Malacca", "Røgede sild", "Spegesild", "Zaanse koeken", "Chocolade",
                                          "Maxilaku", "Valkoinen suklaa", "Filo Mix", "Perth Pasties", "Tourtière",
                                          "Pâté chinois", "Ravioli Angelo", "Gudbrandsdalsost", "Outback Lager",
                                          "Fløtemysost", "Röd Kaviar", "Longlife Tofu", "Lakkalikööri"};
	

		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public Hashtable LoadProducts()
		{			          
			try
			{
				_keyVals = new Hashtable();

                int productID = 0;
	            Random rand = new Random();
				while(productID < _ProductName.Length)
				{
					Product product = new Product();

					product.ProductID		= productID++;
					product.Name		    = _ProductName[productID];
                    product.Supplier        = (int)rand.Next(1, 10);
					product.Category		= (int)rand.Next(1, 8);
					product.UnitsAvailable	= (int)rand.Next(1, 100);
					
					_keyVals[product.ProductID] = product;
				}
			}
			catch(Exception e)
			{
				System.Diagnostics.Trace.WriteLine(e);
			}
		
			return _keyVals;
		}
	}
}
