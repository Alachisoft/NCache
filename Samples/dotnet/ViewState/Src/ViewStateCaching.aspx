<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ViewStateCaching.aspx.cs" Inherits="ViewStateCaching" EnableViewState="true" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html>
<head>
    <title>View State Caching Sample</title>
    <meta content="False" name="vs_snapToGrid">
    <meta content="True" name="vs_showGrid">
    <meta content="Microsoft Visual Studio .NET 7.1" name="GENERATOR">
    <meta content="C#" name="CODE_LANGUAGE">
    <meta content="JavaScript" name="vs_defaultClientScript">
    <meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
</head>
<body>
    <form id="ProductDetailForm" method="post" runat="server">
        <asp:Label ID="lblTitle" runat="server" Font-Size="Small" Font-Names="Tahoma" ForeColor="White"
            Height="23px" Width="800px" Font-Bold="True" BackColor="Gray" BorderStyle="Outset">Using NCache's View State cache implementation</asp:Label><br>
        <br />
        <asp:Label ID="lblDiscription" runat="server" Font-Size="Small" Font-Names="Tahoma"
            ForeColor="Black" Height="32px" Width="600px" Font-Bold="True" BackColor="AliceBlue"
            BorderStyle="Ridge" BorderColor="White" BorderWidth="1px" Design_Time_Lock="True">Below is list of Customers from Northwind database. Select Customer to view details.</asp:Label>
        <br />
        <br />
        <asp:GridView ID="grdCustomers" AutoGenerateColumns="false" runat="server"
            Font-Size="Small" Font-Names="Tahoma" Width="800px" BorderStyle="Ridge" BorderWidth="2px"
            Design_Time_Lock="True" AllowPaging="true" PageSize="7"
            OnRowCommand="grdProductDetail_RowCommand"
            OnPageIndexChanging="grdCustomers_PageIndexChanging">
            <HeaderStyle Font-Bold="True" ForeColor="White" BackColor="#507CD1"></HeaderStyle>
            <Columns>
                <asp:TemplateField>
                    <ItemTemplate>
                        <asp:LinkButton ID="imgbutton" runat="server" CausesValidation="false" CommandName="ShowOrders"
                            Text="Select" CommandArgument='<%#Eval("CustomerID") %>'
                            ToolTip="delete file" />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="CustomerID" HeaderText="Customer ID" />
                <asp:BoundField DataField="ContactName" HeaderText="Name" />
                <asp:BoundField DataField="City" HeaderText="City" />
                <asp:BoundField DataField="PostalCode" HeaderText="Postal Code" />
                <asp:BoundField DataField="Country" HeaderText="Country" />
                <asp:BoundField DataField="Phone" HeaderText="Phone" />
            </Columns>
        </asp:GridView>
        <p>
            <asp:Label ID="lblCustomerIdInfo" runat="server" Text="Orders of Customer Id: " Visible="false"></asp:Label>
            <asp:Label ID="lblCustomerId" runat="server" Text="" Visible="false"></asp:Label>
        </p>
        <asp:GridView ID="grdOrders" AutoGenerateColumns="false" runat="server"
            Font-Size="Small" Font-Names="Tahoma" Width="800px" BorderStyle="Ridge" BorderWidth="2px"
            Design_Time_Lock="True" AllowPaging="true" PageSize="7" Visible="false"
            OnRowCommand="grdOders_RowCommand"
            OnPageIndexChanging="grdOders_PageIndexChanging">
            <HeaderStyle Font-Bold="True" ForeColor="White" BackColor="#507CD1"></HeaderStyle>
            <Columns>
                <asp:TemplateField HeaderText="Command">
                    <ItemTemplate>
                        <asp:LinkButton ID="imgbutton" runat="server" CausesValidation="false" CommandName="OrderDetails"
                            Text="Select" CommandArgument='<%#Eval("OrderID") %>'
                            ToolTip="delete file" />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="OrderID" HeaderText="Order ID" />
                <asp:BoundField DataField="OrderDate" HeaderText="Order Date" />
                <asp:BoundField DataField="RequiredDate" HeaderText="Required Date" />
                <asp:BoundField DataField="ShipName" HeaderText="Ship Name" />
                <asp:BoundField DataField="ShipCity" HeaderText="Ship City" />
                <asp:BoundField DataField="ShipCountry" HeaderText="Ship Country" />
            </Columns>
        </asp:GridView>
        <p>
            <asp:Label ID="lblOrderIdInfo" runat="server" Text="Orders details of Order Id:" Visible="false"></asp:Label>
            <asp:Label ID="lblOrderId" runat="server" Text="" Visible="false"></asp:Label>
        </p>
        <asp:GridView ID="grdOrderDetails" AutoGenerateColumns="false" runat="server"
            Font-Size="Small" Font-Names="Tahoma" Width="800px" BorderStyle="Ridge" BorderWidth="2px"
            Design_Time_Lock="True" AllowPaging="true" PageSize="7" Visible="false"
            OnPageIndexChanging="grdOrderDetails_PageIndexChanging">
            <HeaderStyle Font-Bold="True" ForeColor="White" BackColor="#507CD1"></HeaderStyle>
            <Columns>
                <asp:BoundField DataField="ProductID" HeaderText="Order ID" />
                <asp:BoundField DataField="UnitPrice" HeaderText="Unit Price" />
                <asp:BoundField DataField="Quantity" HeaderText="Quantity" />
                <asp:BoundField DataField="Discount" HeaderText="Discount" />
            </Columns>
        </asp:GridView>
    </form>
</body>
</html>
