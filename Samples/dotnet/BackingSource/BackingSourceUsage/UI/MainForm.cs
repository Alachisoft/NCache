// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System.Configuration;
using System.Windows.Forms;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Sample.Data;

namespace Alachisoft.NCache.Samples.UI
{
	/// <summary>
	/// Summary description for DirectoryApplicationForm.
	/// </summary>
	internal class MainForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TextBox txtCompanyName;
		private System.Windows.Forms.TextBox txtPostalCode;
		private System.Windows.Forms.TextBox txtCountry;
		private System.Windows.Forms.TextBox txtAddress;
		private System.Windows.Forms.TextBox txtFax;
		private System.Windows.Forms.TextBox txtPhone;
		private System.Windows.Forms.TextBox txtContactName;
		private System.Windows.Forms.TextBox txtCity;
		private System.Windows.Forms.GroupBox PostalAddress;
		private System.Windows.Forms.GroupBox ContactInformation;
		private System.Windows.Forms.ComboBox cboCustomerID;
		private System.Windows.Forms.Button cmdUpdate;
		private System.Windows.Forms.Button cmdFind;
		private Label label10;
		private ComboBox cboReadThruProvider;
		private ComboBox cboWriteThruProvider;
		private Label label11;

        private Cache _cache;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public MainForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
            string cacheName = ConfigurationManager.AppSettings["CacheID"];
            if (string.IsNullOrEmpty(cacheName))
            {
                MessageBox.Show("CacheID cannot be null or empty string", Program.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _cache = Alachisoft.NCache.Web.Caching.NCache.InitializeCache(cacheName);

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
            if (_cache != null)
                _cache.Dispose();
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.cboCustomerID = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.txtCompanyName = new System.Windows.Forms.TextBox();
            this.cmdFind = new System.Windows.Forms.Button();
            this.cmdUpdate = new System.Windows.Forms.Button();
            this.PostalAddress = new System.Windows.Forms.GroupBox();
            this.txtCity = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtPostalCode = new System.Windows.Forms.TextBox();
            this.txtCountry = new System.Windows.Forms.TextBox();
            this.txtAddress = new System.Windows.Forms.TextBox();
            this.ContactInformation = new System.Windows.Forms.GroupBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.txtFax = new System.Windows.Forms.TextBox();
            this.txtPhone = new System.Windows.Forms.TextBox();
            this.txtContactName = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.cboReadThruProvider = new System.Windows.Forms.ComboBox();
            this.cboWriteThruProvider = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.PostalAddress.SuspendLayout();
            this.ContactInformation.SuspendLayout();
            this.SuspendLayout();
            // 
            // cboCustomerID
            // 
            this.cboCustomerID.Items.AddRange(new object[] {
            "ALFKI",
            "ANATR",
            "ANTON",
            "AROUT",
            "BERGS",
            "BLAUS",
            "BLONP",
            "BOLID",
            "BONAP",
            "BOTTM",
            "BSBEV",
            "CACTU",
            "CENTC",
            "CHOPS",
            "COMMI",
            "CONSH",
            "DRACD",
            "DUMON",
            "EASTC",
            "ERNSH",
            "FAMIA",
            "FISSA",
            "FOLIG",
            "FOLKO",
            "FRANK",
            "FRANR",
            "FRANS",
            "FURIB",
            "GALED",
            "GODOS",
            "GOURL",
            "GREAL",
            "GROSR",
            "HANAR",
            "HILAA",
            "HUNGC",
            "HUNGO",
            "ISLAT",
            "KOENE",
            "LACOR",
            "LAMAI",
            "LAUGB",
            "LAZYK",
            "LEHMS",
            "LETSS",
            "LILAS",
            "LINOD",
            "LONEP",
            "MAGAA",
            "MAISD",
            "MEREP",
            "MORGK",
            "NORTS",
            "OCEAN",
            "OLDWO",
            "OTTIK",
            "PARIS",
            "PERIC",
            "PICCO",
            "PRINI",
            "QUEDE",
            "QUEEN",
            "QUICK",
            "RANCH",
            "RATTC",
            "REGGC",
            "RICAR",
            "RICSU",
            "ROMEY",
            "SANTG",
            "SAVEA",
            "SEVES",
            "SIMOB",
            "SPECD",
            "SPLIR",
            "SUPRD",
            "THEBI",
            "THECR",
            "TOMSP",
            "TORTU",
            "TRADH",
            "TRAIH",
            "VAFFE",
            "VICTE",
            "VINET",
            "WANDK",
            "WARTH",
            "WELLI",
            "WHITC",
            "WILMK",
            "WOLZA"});
            this.cboCustomerID.Location = new System.Drawing.Point(120, 42);
            this.cboCustomerID.Name = "cboCustomerID";
            this.cboCustomerID.Size = new System.Drawing.Size(151, 21);
            this.cboCustomerID.TabIndex = 1;
            this.cboCustomerID.SelectedIndexChanged += new System.EventHandler(this.OnClickFind);
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(16, 46);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Customer ID";
            this.label1.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(16, 76);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(96, 16);
            this.label2.TabIndex = 3;
            this.label2.Text = "Customer Name";
            this.label2.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // label9
            // 
            this.label9.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(16, 99);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(96, 16);
            this.label9.TabIndex = 5;
            this.label9.Text = "Company Name";
            this.label9.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // txtCompanyName
            // 
            this.txtCompanyName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtCompanyName.Location = new System.Drawing.Point(120, 96);
            this.txtCompanyName.MaxLength = 256;
            this.txtCompanyName.Name = "txtCompanyName";
            this.txtCompanyName.Size = new System.Drawing.Size(224, 21);
            this.txtCompanyName.TabIndex = 6;
            // 
            // cmdFind
            // 
            this.cmdFind.BackColor = System.Drawing.Color.LightGray;
            this.cmdFind.Location = new System.Drawing.Point(280, 40);
            this.cmdFind.Name = "cmdFind";
            this.cmdFind.Size = new System.Drawing.Size(64, 23);
            this.cmdFind.TabIndex = 2;
            this.cmdFind.Text = "&Find";
            this.cmdFind.UseVisualStyleBackColor = false;
            this.cmdFind.Click += new System.EventHandler(this.OnClickFind);
            // 
            // cmdUpdate
            // 
            this.cmdUpdate.BackColor = System.Drawing.Color.LightGray;
            this.cmdUpdate.Location = new System.Drawing.Point(120, 423);
            this.cmdUpdate.Name = "cmdUpdate";
            this.cmdUpdate.Size = new System.Drawing.Size(75, 23);
            this.cmdUpdate.TabIndex = 9;
            this.cmdUpdate.Text = "&Update";
            this.cmdUpdate.UseVisualStyleBackColor = false;
            this.cmdUpdate.Click += new System.EventHandler(this.OnClickUpdate);
            // 
            // PostalAddress
            // 
            this.PostalAddress.Controls.Add(this.txtCity);
            this.PostalAddress.Controls.Add(this.label6);
            this.PostalAddress.Controls.Add(this.label5);
            this.PostalAddress.Controls.Add(this.label4);
            this.PostalAddress.Controls.Add(this.label3);
            this.PostalAddress.Controls.Add(this.txtPostalCode);
            this.PostalAddress.Controls.Add(this.txtCountry);
            this.PostalAddress.Controls.Add(this.txtAddress);
            this.PostalAddress.Location = new System.Drawing.Point(16, 126);
            this.PostalAddress.Name = "PostalAddress";
            this.PostalAddress.Size = new System.Drawing.Size(344, 166);
            this.PostalAddress.TabIndex = 7;
            this.PostalAddress.TabStop = false;
            this.PostalAddress.Text = "Postal Address";
            // 
            // txtCity
            // 
            this.txtCity.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtCity.Location = new System.Drawing.Point(102, 85);
            this.txtCity.MaxLength = 256;
            this.txtCity.Name = "txtCity";
            this.txtCity.Size = new System.Drawing.Size(144, 21);
            this.txtCity.TabIndex = 8;
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(10, 138);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(72, 16);
            this.label6.TabIndex = 6;
            this.label6.Text = "Postal Code";
            this.label6.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(10, 114);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(72, 16);
            this.label5.TabIndex = 4;
            this.label5.Text = "Country";
            this.label5.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(10, 90);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(32, 16);
            this.label4.TabIndex = 2;
            this.label4.Text = "City";
            this.label4.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(10, 24);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(46, 16);
            this.label3.TabIndex = 0;
            this.label3.Text = "Address";
            this.label3.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // txtPostalCode
            // 
            this.txtPostalCode.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPostalCode.Location = new System.Drawing.Point(102, 134);
            this.txtPostalCode.MaxLength = 8;
            this.txtPostalCode.Name = "txtPostalCode";
            this.txtPostalCode.Size = new System.Drawing.Size(144, 21);
            this.txtPostalCode.TabIndex = 7;
            // 
            // txtCountry
            // 
            this.txtCountry.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtCountry.Location = new System.Drawing.Point(102, 109);
            this.txtCountry.MaxLength = 256;
            this.txtCountry.Name = "txtCountry";
            this.txtCountry.Size = new System.Drawing.Size(144, 21);
            this.txtCountry.TabIndex = 5;
            // 
            // txtAddress
            // 
            this.txtAddress.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtAddress.Location = new System.Drawing.Point(10, 40);
            this.txtAddress.MaxLength = 2048;
            this.txtAddress.Multiline = true;
            this.txtAddress.Name = "txtAddress";
            this.txtAddress.Size = new System.Drawing.Size(320, 40);
            this.txtAddress.TabIndex = 1;
            // 
            // ContactInformation
            // 
            this.ContactInformation.Controls.Add(this.label8);
            this.ContactInformation.Controls.Add(this.label7);
            this.ContactInformation.Controls.Add(this.txtFax);
            this.ContactInformation.Controls.Add(this.txtPhone);
            this.ContactInformation.Location = new System.Drawing.Point(16, 301);
            this.ContactInformation.Name = "ContactInformation";
            this.ContactInformation.Size = new System.Drawing.Size(344, 80);
            this.ContactInformation.TabIndex = 8;
            this.ContactInformation.TabStop = false;
            this.ContactInformation.Text = "Contact Information";
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(10, 49);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(72, 16);
            this.label8.TabIndex = 2;
            this.label8.Text = "Fax number";
            this.label8.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(10, 27);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(88, 16);
            this.label7.TabIndex = 0;
            this.label7.Text = "Phone number";
            this.label7.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // txtFax
            // 
            this.txtFax.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtFax.Location = new System.Drawing.Point(102, 48);
            this.txtFax.MaxLength = 12;
            this.txtFax.Name = "txtFax";
            this.txtFax.Size = new System.Drawing.Size(144, 21);
            this.txtFax.TabIndex = 3;
            // 
            // txtPhone
            // 
            this.txtPhone.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPhone.Location = new System.Drawing.Point(102, 22);
            this.txtPhone.MaxLength = 12;
            this.txtPhone.Name = "txtPhone";
            this.txtPhone.Size = new System.Drawing.Size(144, 21);
            this.txtPhone.TabIndex = 1;
            // 
            // txtContactName
            // 
            this.txtContactName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtContactName.Location = new System.Drawing.Point(120, 71);
            this.txtContactName.MaxLength = 256;
            this.txtContactName.Name = "txtContactName";
            this.txtContactName.Size = new System.Drawing.Size(224, 21);
            this.txtContactName.TabIndex = 4;
            // 
            // label10
            // 
            this.label10.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(16, 17);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(96, 16);
            this.label10.TabIndex = 10;
            this.label10.Text = "Select Provider";
            this.label10.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // cboReadThruProvider
            // 
            this.cboReadThruProvider.Items.AddRange(new object[] {
            "Default",
            "SqlReadThruProvider",
            "SqliteReadThruProvider"});
            this.cboReadThruProvider.Location = new System.Drawing.Point(120, 12);
            this.cboReadThruProvider.Name = "cboReadThruProvider";
            this.cboReadThruProvider.Size = new System.Drawing.Size(151, 21);
            this.cboReadThruProvider.TabIndex = 11;
            this.cboReadThruProvider.Text = "Default";
            // 
            // cboWriteThruProvider
            // 
            this.cboWriteThruProvider.Items.AddRange(new object[] {
            "Default",
            "SqlWriteThruProvider",
            "SqliteWriteThruProvider"});
            this.cboWriteThruProvider.Location = new System.Drawing.Point(118, 391);
            this.cboWriteThruProvider.Name = "cboWriteThruProvider";
            this.cboWriteThruProvider.Size = new System.Drawing.Size(151, 21);
            this.cboWriteThruProvider.TabIndex = 13;
            this.cboWriteThruProvider.Text = "Default";
            // 
            // label11
            // 
            this.label11.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(14, 396);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(96, 16);
            this.label11.TabIndex = 12;
            this.label11.Text = "Select Provider";
            this.label11.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // MainForm
            // 
            this.AcceptButton = this.cmdFind;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(369, 457);
            this.Controls.Add(this.cboWriteThruProvider);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.cboReadThruProvider);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.PostalAddress);
            this.Controls.Add(this.ContactInformation);
            this.Controls.Add(this.cmdUpdate);
            this.Controls.Add(this.txtCompanyName);
            this.Controls.Add(this.txtContactName);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmdFind);
            this.Controls.Add(this.cboCustomerID);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Customer Directory";
            this.PostalAddress.ResumeLayout(false);
            this.PostalAddress.PerformLayout();
            this.ContactInformation.ResumeLayout(false);
            this.ContactInformation.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		/// <summary>
		/// The records are fetched from according to the Customer ID
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnClickFind(object sender, System.EventArgs e)
		{
            Customer customer = new Customer();

			if (cboReadThruProvider.SelectedItem.ToString().Equals("Default"))
                customer = (Customer)_cache.Get(cboCustomerID.Text.Trim().ToUpper(), DSReadOption.ReadThru);			
			else
                customer = (Customer)_cache.Get(cboCustomerID.Text.Trim().ToUpper(), cboReadThruProvider.SelectedItem.ToString(), DSReadOption.ReadThru);			
			if(customer == null)
			{
				MessageBox.Show(this, "Customer information does not exist",
					Program.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			txtContactName.Text = customer.ContactName;
			txtCompanyName.Text = customer.CompanyName;
			txtAddress.Text		= customer.Address;
			txtCity.Text		= customer.City;
			txtCountry.Text		= customer.Country;
			txtPostalCode.Text	= customer.PostalCode;
			txtPhone.Text		= customer.ContactNo;
			txtFax.Text			= customer.Fax;

			if(!cboCustomerID.Items.Contains(cboCustomerID.Text))
				cboCustomerID.Items.Add(cboCustomerID.Text);
		}
		/// <summary>
		/// Records are updated when this button is clicked.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnClickUpdate(object sender, System.EventArgs e)
		{
			Customer customer = new Customer();

			customer.CustomerID		= cboCustomerID.Text.Trim();
			customer.ContactName	= txtContactName.Text;
			customer.CompanyName	= txtCompanyName.Text;
			customer.Address		= txtAddress.Text;
			customer.City			= txtCity.Text;
			customer.Country		= txtCountry.Text;
			customer.PostalCode		= txtPostalCode.Text;
			customer.ContactNo			= txtPhone.Text;
			customer.Fax			= txtFax.Text;

			if (cboWriteThruProvider.SelectedItem.ToString().Equals("Default"))
                _cache.Insert(customer.CustomerID.ToUpper(), new CacheItem(customer), DSWriteOption.WriteThru, null);
			else
                _cache.Insert(customer.CustomerID.ToUpper(), new CacheItem(customer), DSWriteOption.WriteThru, cboWriteThruProvider.Text, null);
			if(!cboCustomerID.Items.Contains(customer.CustomerID))
				cboCustomerID.Items.Add(customer.CustomerID);

			MessageBox.Show(this, "Customer information updated successfuly",
				Program.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
