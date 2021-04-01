<%@ Page language="c#" Inherits="NOutputCacheSample.MainForm" CodeFile="Main.aspx.cs" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN" >
<HTML>
	<HEAD>
		<title>Output Cache Sample</title>
		<meta content="False" name="vs_snapToGrid">
		<meta content="True" name="vs_showGrid">
		<meta content="Microsoft Visual Studio .NET 7.1" name="GENERATOR">
		<meta content="C#" name="CODE_LANGUAGE">
		<meta content="JavaScript" name="vs_defaultClientScript">
		<meta content="http://schemas.microsoft.com/intellisense/ie5" name="vs_targetSchema">
	</HEAD>
	<body>
		<form id="CustomerDetailForm" method="post" runat="server">
			<asp:label id="lblTitle" runat="server" Font-Size="X-Small" Font-Names="Tahoma" ForeColor="White"
				Height="23px" Width="800px" Font-Bold="True" BackColor="Gray" BorderStyle="Outset">Using NCache's ouput cache implementation</asp:label><br>
			<br>
			<asp:label id="lblPageCreation" style="Z-INDEX: 105; LEFT: 16px; POSITION: absolute; TOP: 131px; Design_Time_Lock: True"
				runat="server" Font-Size="X-Small" Font-Names="Tahoma" Design_Time_Lock="True">Page Creation Time:</asp:label><asp:label id="lblPageCreationTime" style="Z-INDEX: 106; LEFT: 168px; POSITION: absolute; TOP: 130px; Design_Time_Lock: True"
				runat="server" Font-Size="X-Small" Font-Names="Tahoma" ForeColor="Red" Design_Time_Lock="True">Label</asp:label><asp:label id="lblDiscription" style="Z-INDEX: 107; LEFT: 12px; POSITION: absolute; TOP: 56px; Design_Time_Lock: True"
				runat="server" Font-Size="X-Small" Font-Names="Tahoma" ForeColor="Black" Height="32px" Width="800px" Font-Bold="True" BackColor="AliceBlue" BorderStyle="Ridge" BorderColor="White" BorderWidth="1px" Design_Time_Lock="True">The selected customer's page will be cached. On request of same customer's record the page will be returned from NCache or will execute if it is expired from cache. The page creation time will determine when the page is executed.</asp:label><br>
			<asp:datagrid id="grdCustomerDetail" style="Z-INDEX: 102; LEFT: 15px; POSITION: absolute; TOP: 208px; Design_Time_Lock: True"
				runat="server" Font-Size="X-Small" Font-Names="Tahoma" Width="800px" Caption="Customer Details:"
				CaptionAlign="Left" BorderStyle="Ridge" BorderWidth="2px" Design_Time_Lock="True">
				<ItemStyle BorderColor="DarkGray" BackColor="AliceBlue"></ItemStyle>
				<HeaderStyle Font-Bold="True" ForeColor="White" BackColor="#507CD1"></HeaderStyle>
			</asp:datagrid><br>
			<asp:button id="btnShowCustomerDetail" style="Z-INDEX: 101; LEFT: 323px; POSITION: absolute; TOP: 178px; Design_Time_Lock: True"
				runat="server" Font-Size="X-Small" Font-Names="Tahoma" Width="134px" Text="Show Details"
				BorderStyle="Solid" BorderWidth="1px" Design_Time_Lock="True" onclick="OnbtnShowCustomerDetail"></asp:button><asp:label id="lblSelectCustomer" style="Z-INDEX: 103; LEFT: 16px; POSITION: absolute; TOP: 181px; Design_Time_Lock: True"
				runat="server" Font-Size="X-Small" Font-Names="Tahoma" Design_Time_Lock="True">Select Customer Name:</asp:label><asp:dropdownlist id="cbxCustomerName" style="Z-INDEX: 104; LEFT: 168px; POSITION: absolute; TOP: 178px; Design_Time_Lock: True"
				runat="server" Font-Size="X-Small" Font-Names="Tahoma" Width="128px" Design_Time_Lock="True"></asp:dropdownlist></form>
	</body>
</HTML>
