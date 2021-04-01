//===============================================================================
// Copyright© 2014 Alachisoft.  All rights reserved.
//===============================================================================

using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Data.OleDb;
using System.Configuration;

namespace NOutputCacheSample
{
	/// <summary>
	/// Summary description for MainForm.
	/// </summary>
	public partial class MainForm : System.Web.UI.Page
	{
		protected System.Web.UI.WebControls.Label Label1;
		protected System.Web.UI.WebControls.Label Label2;
		protected System.Web.UI.WebControls.Label Label3;
		
		private OleDbConnection cn;
		private OleDbDataReader dr;
		private OleDbCommand cmd;
		private OleDbDataAdapter da;
		private DataSet ds;
		
		private string connectionString;
		string customerID;


		protected void Page_Load(object sender, System.EventArgs e)
		{
			connectionString = ConfigurationSettings.AppSettings["conn-string"].ToString();
			
			cn = new OleDbConnection(connectionString);
			cn.Open();
			cmd = new OleDbCommand("SELECT CustomerID FROM Customers", cn);
			dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

			while(dr.Read())
			{
			this.cbxCustomerName.Items.Add(dr["CustomerID"].ToString());
			}
	
			dr.Close();

			
			if(this.IsPostBack)
			{
				customerID = Request.Form["cbxCustomerName"].ToString();
			}
			else 
			{
				// Show details of first customer in case the page loads for the first time
				
				customerID = this.cbxCustomerName.SelectedValue;

				da = new OleDbDataAdapter("SELECT CompanyName, Address, City, Country, Phone FROM Customers WHERE (CustomerID = '" + this.customerID + "')", cn);
				ds = new DataSet("CustomerDataSet");
				da.Fill(ds, "Customers");
		
				grdCustomerDetail.DataSource = ds;
				grdCustomerDetail.DataBind();

			}

			cn.Close();

			this.lblPageCreationTime.Text = DateTime.Now.ToString();
		}

		#region Web Form Designer generated code
		override protected void OnInit(EventArgs e)
		{
			//
			// CODEGEN: This call is required by the ASP.NET Web Form Designer.
			//
			InitializeComponent();
			base.OnInit(e);
		}
		
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{    

		}
		#endregion

		protected void OnbtnShowCustomerDetail(object sender, System.EventArgs e)
		{
			connectionString = ConfigurationSettings.AppSettings["conn-string"].ToString();
			
			cn = new OleDbConnection(connectionString);
			cn.Open();
			
			da = new OleDbDataAdapter("SELECT CompanyName, Address, City, Country, Phone FROM Customers WHERE (CustomerID = '" + this.customerID + "')", cn);
			ds = new DataSet("CustomerDataSet");
			da.Fill(ds, "Customers");
		
			grdCustomerDetail.DataSource = ds;
			grdCustomerDetail.DataBind();

			cn.Close();
		}
	}
}
