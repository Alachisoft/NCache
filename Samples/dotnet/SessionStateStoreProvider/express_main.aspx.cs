// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// NCache Session state provider sample
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

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
using Alachisoft.NCache.Web.SessionState;

namespace guessgame
{
	/// <summary>
	/// Summary description for WebForm1.
	/// </summary>
	public partial class MainForm : System.Web.UI.Page
	{
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

		private void StartNewGame()
		{
			Session.Clear();

			Random rndObject = new Random();
			Session["SecretNumber"] = rndObject.Next(1, 100);
			Session["AttemptsCount"] = 0;
			
			cmdGuess.Text = "Guess!";		
			txtGuess.Enabled = true;
			lblHint.Text = "Hint: The number is between 0 and 101.";
			lblMessage.Text = @"You haven't made any attempt yet.";
			lblLastGuess.Text = "None";
			lblHistory.Text = "None";
		}

		protected void Page_Load(object sender, System.EventArgs e)
		{
			this.lblSystemName.Text = "Page generated at: " + Environment.MachineName;
			if(!this.IsPostBack)
			{
				StartNewGame();
			}
		}

		protected void OnClickGuess(object sender, System.EventArgs e)
		{
			if (cmdGuess.Text == "New Game")
			{
				StartNewGame();
				return;
			}

			int guess = 0;
			try
			{
				guess = Convert.ToInt32(txtGuess.Text);
				txtGuess.Text = string.Empty;
			}
			catch(Exception)
			{
				lblMessage.Text = "Please specify a valid number.";
				return;
			}

			int secretNumber = Convert.ToInt32(Session["SecretNumber"]);
			int attemptsCount = Convert.ToInt32(Session["AttemptsCount"]);

			attemptsCount++;

			lblLastGuess.Text = guess.ToString();
			lblMessage.Text = "#" + attemptsCount + ": ";
			if (guess > secretNumber)
			{				
				lblMessage.Text += "The number that you have tried is greater than the guess.";
			}
			else if (guess < secretNumber)
			{
				lblMessage.Text += "The number that you have tried is lesser than the guess.";
			}
			else
			{
				lblMessage.Text += "You have found the Secret Number";
				txtGuess.Enabled = false;
				cmdGuess.Text = "New Game";
                
			}
			Session[attemptsCount.ToString()] = lblLastGuess.Text;			
			Session["AttemptsCount"] = attemptsCount;

			PopulateHistory(secretNumber);
            		Session.Timeout = 180;
            		}


		/// <summary>
		/// Populate list box with history
		/// It is to verify the working of distributed session
		/// </summary>
		private void PopulateHistory(int secret)
		{
			string attempts = string.Empty;
			int attemptsCount = Convert.ToInt32(Session["AttemptsCount"]);

			int min = 0, max = 101;
			for (int i=1; i<=attemptsCount; i++)
			{
				if(i > 1) attempts += ", ";
				int val = Convert.ToInt32(Session[i.ToString()]);

				if(val < secret && val > min) min = val;
				if(val > secret && val < max) max = val;

				attempts += val.ToString();
			}
			lblHistory.Text = attempts;
			lblHint.Text = "Hint: The number is between " + 
				min + " and " + max + ".";
		}
	}
}
