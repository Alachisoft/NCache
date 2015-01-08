<%@ Page language="c#" Inherits="guessgame.MainForm" CodeFile="main.aspx.cs" %>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN" >
<HTML>
	<HEAD>
		<title>Guessing Game</title>
		<meta name="GENERATOR" Content="Microsoft Visual Studio .NET 7.1">
		<meta name="CODE_LANGUAGE" Content="C#">
		<meta name="vs_defaultClientScript" content="JavaScript">
		<meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
	</HEAD>
	<body>
		<form id="Form1" method="post" runat="server">
			<asp:Label id="Label1" style="Z-INDEX: 100; LEFT: 22px; POSITION: absolute; TOP: 58px" runat="server"
				Height="24px" Width="454px" Font-Bold="True" Font-Names="Tahoma" Font-Size="Smaller" BackColor="Wheat">The computer has selected a random number between 1 and 100 (inclusive). In this game you have to try to guess the number.</asp:Label>
			<asp:Label id="Label6" style="Z-INDEX: 112; LEFT: 24px; POSITION: absolute; TOP: 271px" runat="server"
				Font-Size="X-Small" Font-Names="Tahoma" Width="96px" BackColor="Transparent">Last Guess:</asp:Label>
			<asp:Label id="lblHistory" style="Z-INDEX: 111; LEFT: 170px; POSITION: absolute; TOP: 295px"
				runat="server" Font-Size="Smaller" Font-Names="Tahoma" Font-Bold="True" Width="304px" Height="72px">None</asp:Label>
			<asp:Label id="Label5" style="Z-INDEX: 110; LEFT: 22px; POSITION: absolute; TOP: 295px" runat="server"
				Font-Size="Smaller" Font-Names="Tahoma">Previous attempts:</asp:Label>
			<asp:TextBox id="txtGuess" style="Z-INDEX: 101; LEFT: 121px; POSITION: absolute; TOP: 130px" runat="server"
				Width="48px" BorderStyle="Solid" BorderWidth="1px" MaxLength="3"></asp:TextBox>
			<asp:Label id="Label2" style="Z-INDEX: 102; LEFT: 22px; POSITION: absolute; TOP: 133px" runat="server"
				Font-Names="Tahoma" Font-Size="Smaller">Enter a number:</asp:Label>
			<asp:Label id="lblHint" style="Z-INDEX: 103; LEFT: 24px; POSITION: absolute; TOP: 199px" runat="server"
				Font-Names="Tahoma" Font-Size="X-Small" Font-Italic="True" BackColor="Transparent">Hint: The number is between 0 and 101.</asp:Label>
			<asp:Label id="lblMessage" style="Z-INDEX: 105; LEFT: 22px; POSITION: absolute; TOP: 168px"
				runat="server" Width="450px" Font-Names="Tahoma" Font-Size="Smaller" ForeColor="Crimson"
				BackColor="Transparent">You haven't made any attempt yet.</asp:Label>
			<asp:Label id="lblLastGuess" style="Z-INDEX: 106; LEFT: 170px; POSITION: absolute; TOP: 271px"
				runat="server" Width="54px" Font-Bold="True" Font-Names="Tahoma" Font-Size="Smaller">None</asp:Label>
			<asp:Button id="cmdGuess" style="Z-INDEX: 107; LEFT: 188px; POSITION: absolute; TOP: 129px" runat="server"
				Width="134px" Text="Guess!" Font-Names="Tahoma" Font-Size="Smaller" BorderStyle="Solid"
				BorderWidth="1px" onclick="OnClickGuess"></asp:Button>
			<asp:Label id="lblSystemName" style="Z-INDEX: 108; LEFT: 22px; POSITION: absolute; TOP: 388px"
				runat="server" Width="454px" Font-Names="Tahoma" Font-Size="X-Small" Font-Italic="True"
				BackColor="Wheat"></asp:Label>
			<asp:Label id="Label4" style="Z-INDEX: 109; LEFT: 22px; POSITION: absolute; TOP: 239px" runat="server"
				Font-Names="Tahoma" Font-Size="X-Small" Font-Bold="True" Width="454px" BackColor="Wheat"> Attempts History ---</asp:Label>
            <asp:Label ID="Label3" style="z-index: auto; left:22px; position: absolute; top: 24px" runat="server"
                Font-Names="Tahoma" Font-Size="X-Small" Font-Bold="True" Width="454px" BackColor="LightSlateGray" BorderStyle="Outset" Font-Overline="False" Font-Strikeout="False" Font-Underline="False" ForeColor="White" Height="23px">Using NCache's Session Store Provider Implementation</asp:Label>
		</form>
	</body>
</HTML>
