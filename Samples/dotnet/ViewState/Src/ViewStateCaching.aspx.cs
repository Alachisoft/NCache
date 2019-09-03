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
using System.Web.UI.WebControls;
using System.Data;
using System.Xml;

public partial class ViewStateCaching : System.Web.UI.Page
{
    protected System.Web.UI.WebControls.Label Label1;
    protected System.Web.UI.WebControls.Label Label2;
    protected System.Web.UI.WebControls.Label Label3;

    private DataSet ds;
    static DataSource datasource = new DataSource();
    static Product[] prod;

    static string ProductID;
    protected void Page_Load(object sender, EventArgs e)
    {
        if (IsPostBack == false)
        {
            DataSet dataSet = new DataSet();
            dataSet.ReadXml(Server.MapPath("App_Data/northwind.xml"));

            grdCustomers.DataSource = dataSet.Tables["Customer"];
            grdCustomers.DataBind();
        }
    }

    protected void grdProductDetail_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName.Equals("ShowOrders"))
        {
            lblCustomerId.Text = e.CommandArgument.ToString();
            DataSet dataSet = new DataSet();
            dataSet.ReadXml(Server.MapPath("App_Data/northwind.xml"));

            DataTable preOrdersTable = dataSet.Tables["Order"]; ;
            DataTable ordersTable = new DataTable("Order");

            foreach (DataColumn tempCol in preOrdersTable.Columns)
            {
                ordersTable.Columns.Add(tempCol.ColumnName);
            }

            DataRow[] dRows = dataSet.Tables["Order"].Select("CustomerID = '" + e.CommandArgument.ToString() + "'");

            foreach (DataRow tempRow in dRows)
            {
                ordersTable.ImportRow(tempRow);
            }

            grdOrders.DataSource = ordersTable;
            grdOrders.DataBind();

            lblCustomerIdInfo.Visible = true;
            lblCustomerId.Visible = true;
            grdOrders.Visible = true;
        }
    }

    protected void grdOders_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName.Equals("OrderDetails"))
        {
            lblOrderId.Text = e.CommandArgument.ToString();
            DataTable dt = GetOrderDetailsByOrderId(Int32.Parse(e.CommandArgument.ToString()));

            grdOrderDetails.DataSource = dt;
            grdOrderDetails.DataBind();

            lblOrderIdInfo.Visible = true;
            lblOrderId.Visible = true;
            grdOrderDetails.Visible = true;
        }
    }
    private DataTable GetOrderDetailsByOrderId(int orderId)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(Server.MapPath("App_Data/northwind.xml"));

        XmlNodeList nodes = doc.DocumentElement.SelectNodes("//Order[@OrderID=" + orderId + "]")[0].SelectNodes("OrderDetails")[0].SelectNodes("OrderDetail");

        DataTable dt = XmlNodeListToDataTable(nodes, new string[] { "ProductID", "UnitPrice", "Quantity", "Discount" });
        return dt;
    }

    public DataTable XmlNodeListToDataTable(XmlNodeList xmlNodeList, string[] Columns)
    {
        // Creating the DataTable.
        using (DataTable dataTable = new DataTable("DataTable"))
        {
            // Adding data Table columns based on the columns parameter
            foreach (string column in Columns)
            {
                dataTable.Columns.Add(column);
            }
            // Adding rows with values.
            DataRow dataRow;
            foreach (XmlNode node in xmlNodeList)
            {
                dataRow = dataTable.NewRow();
                foreach (string column in Columns)
                {
                    dataRow[column] = node.SelectSingleNode(column).InnerText;
                }
                dataTable.Rows.Add(dataRow);
            }
            return dataTable;
        }
    }
    protected void grdCustomers_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        DataSet dataSet = new DataSet();
        dataSet.ReadXml(Server.MapPath("App_Data/northwind.xml"));

        grdCustomers.DataSource = dataSet.Tables["Customer"];
        grdCustomers.DataBind();

        grdCustomers.PageIndex = e.NewPageIndex;
        grdCustomers.DataBind();
    }

    protected void grdOders_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        DataSet dataSet = new DataSet();
        dataSet.ReadXml(Server.MapPath("App_Data/northwind.xml"));

        grdOrders.DataSource = dataSet.Tables["Order"];
        grdOrders.DataBind();

        grdOrders.PageIndex = e.NewPageIndex;
        grdOrders.DataBind();
    }

    protected void grdOrderDetails_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        DataSet dataSet = new DataSet();
        dataSet.ReadXml(Server.MapPath("App_Data/northwind.xml"));

        grdOrderDetails.DataSource = dataSet.Tables["OrderDetail"];
        grdOrderDetails.DataBind();

        grdOrderDetails.PageIndex = e.NewPageIndex;
        grdOrderDetails.DataBind();
    }
}