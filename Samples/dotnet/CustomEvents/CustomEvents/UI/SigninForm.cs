// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System;
using System.Drawing;
using System.Configuration;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Samples.CustomEvents.Utility;

namespace Alachisoft.NCache.Samples.CustomEvents.UI
{
	/// <summary>
	/// Summary description for signin.
	/// </summary>
	internal class SigninForm : System.Windows.Forms.Form
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox screenName;
		private System.Windows.Forms.Button btnSingin;
		private System.Windows.Forms.Label labelNC;
		
		public SigninForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();  
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(SigninForm));
			this.btnSingin = new System.Windows.Forms.Button();
			this.screenName = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.labelNC = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// btnSingin
			// 
			this.btnSingin.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnSingin.BackgroundImage")));
			this.btnSingin.Cursor = System.Windows.Forms.Cursors.Hand;
			this.btnSingin.Location = new System.Drawing.Point(50, 143);
			this.btnSingin.Name = "btnSingin";
			this.btnSingin.Size = new System.Drawing.Size(82, 30);
			this.btnSingin.TabIndex = 1;
			this.btnSingin.Click += new System.EventHandler(this.OnClickSignin);
			// 
			// screenName
			// 
			this.screenName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.screenName.Location = new System.Drawing.Point(50, 117);
			this.screenName.Multiline = true;
			this.screenName.Name = "screenName";
			this.screenName.Size = new System.Drawing.Size(230, 21);
			this.screenName.TabIndex = 0;
			this.screenName.Text = "";
			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.Color.Transparent;
			this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label1.ForeColor = System.Drawing.Color.MidnightBlue;
			this.label1.Location = new System.Drawing.Point(50, 96);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(150, 16);
			this.label1.TabIndex = 2;
			this.label1.Text = "Enter your Screen name:";
			// 
			// labelNC
			// 
			this.labelNC.BackColor = System.Drawing.Color.Transparent;
			this.labelNC.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.labelNC.ForeColor = System.Drawing.SystemColors.WindowText;
			this.labelNC.Location = new System.Drawing.Point(9, 24);
			this.labelNC.Name = "labelNC";
			this.labelNC.Size = new System.Drawing.Size(280, 40);
			this.labelNC.TabIndex = 3;
			this.labelNC.Text = "Welcome to NCache ChatRoom";
			this.labelNC.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// SigninForm
			// 
			this.AcceptButton = this.btnSingin;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
			this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
			this.ClientSize = new System.Drawing.Size(298, 383);
			this.Controls.Add(this.labelNC);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.screenName);
			this.Controls.Add(this.btnSingin);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.Name = "SigninForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "NCache ChatRoom";
			this.ResumeLayout(false);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.OnRoomFormClosing);   
		}
		#endregion

		private string ScreenName
		{
			get { return screenName.Text.Trim(); }
		}

		private void OnClickSignin(object sender, System.EventArgs e)
		{
			string user = ScreenName;
			if(user.Length < 1)
			{
				return;
			}

			object result = null;
			try
			{
                /// Add the user as a non-evictable entity.
                result = NCache.Web.Caching.NCache.Caches[Helper.CacheName].Add("<user>" + user,
					DateTime.UtcNow,
					null,
					Cache.NoAbsoluteExpiration,
					Cache.NoSlidingExpiration,
					CacheItemPriority.NotRemovable
                    );

                /// Send a custom notification so that existing users can know that a new user has
                /// entered the conversation. The notification is sent asynchroneously.
                NCache.Web.Caching.NCache.Caches[Helper.CacheName].RaiseCustomEvent(Msg.Code.NewUser, user);
			}
			catch(Exception) /// If exceptions are enabled.
			{
			}
			if (result == null) 
			{
				MessageBox.Show(this,
					"The Screen name you have choosen is not available. Please select some other Screen name ! ",
                    Program.Title, 
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
				return;
			}
			
			this.Visible = false;
			RoomForm objRoom = new RoomForm(user);
			objRoom.ShowDialog();
			this.Visible = true;
		}

        private void OnRoomFormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            NCache.Web.Caching.NCache.Caches[Helper.CacheName].Dispose();
        }
	}
}