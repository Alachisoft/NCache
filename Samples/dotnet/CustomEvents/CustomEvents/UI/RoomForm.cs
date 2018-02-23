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

using Alachisoft.NCache.Samples.CustomEvents.Utility;
using Alachisoft.NCache.Web.Caching;

namespace Alachisoft.NCache.Samples.CustomEvents.UI
{
	/// <summary>
	/// Summary description for RoomForm.
	/// </summary>
	internal class RoomForm : System.Windows.Forms.Form
	{
		private System.ComponentModel.IContainer components = null;
		private System.Windows.Forms.ListBox listUsers;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.RichTextBox txtRoom;
		private System.Windows.Forms.TextBox txtMsg;
		private System.Windows.Forms.Button btnSend;
		private System.Windows.Forms.Label lblWelcome;
		private System.Windows.Forms.Button btnSignout;
        public string _userName;
        private CustomEventCallback _appEvent;
		
        public RoomForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

            NCache.Web.Caching.NCache.Caches[Helper.CacheName].ExceptionsEnabled = true;            
		}

		public RoomForm(string name)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			_userName = name;
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RoomForm));
            this.listUsers = new System.Windows.Forms.ListBox();
            this.txtMsg = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtRoom = new System.Windows.Forms.RichTextBox();
            this.lblWelcome = new System.Windows.Forms.Label();
            this.btnSignout = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listUsers
            // 
            this.listUsers.BackColor = System.Drawing.SystemColors.Window;
            this.listUsers.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listUsers.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listUsers.ForeColor = System.Drawing.Color.SteelBlue;
            this.listUsers.IntegralHeight = false;
            this.listUsers.Location = new System.Drawing.Point(405, 80);
            this.listUsers.Name = "listUsers";
            this.listUsers.Size = new System.Drawing.Size(128, 312);
            this.listUsers.Sorted = true;
            this.listUsers.TabIndex = 4;
            // 
            // txtMsg
            // 
            this.txtMsg.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtMsg.Location = new System.Drawing.Point(21, 449);
            this.txtMsg.Multiline = true;
            this.txtMsg.Name = "txtMsg";
            this.txtMsg.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtMsg.Size = new System.Drawing.Size(441, 50);
            this.txtMsg.TabIndex = 1;
            // 
            // btnSend
            // 
            this.btnSend.BackColor = System.Drawing.SystemColors.Window;
            this.btnSend.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnSend.BackgroundImage")));
            this.btnSend.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSend.Location = new System.Drawing.Point(482, 450);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(54, 52);
            this.btnSend.TabIndex = 3;
            this.btnSend.TabStop = false;
            this.btnSend.UseVisualStyleBackColor = false;
            this.btnSend.Click += new System.EventHandler(this.OnClickSend);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.SteelBlue;
            this.label1.Location = new System.Drawing.Point(22, 56);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 16);
            this.label1.TabIndex = 4;
            this.label1.Text = "Messages";
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.SteelBlue;
            this.label2.Location = new System.Drawing.Point(396, 56);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(76, 16);
            this.label2.TabIndex = 5;
            this.label2.Text = "Whos online";
            // 
            // txtRoom
            // 
            this.txtRoom.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.txtRoom.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtRoom.HideSelection = false;
            this.txtRoom.Location = new System.Drawing.Point(26, 95);
            this.txtRoom.Name = "txtRoom";
            this.txtRoom.ReadOnly = true;
            this.txtRoom.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.txtRoom.Size = new System.Drawing.Size(357, 300);
            this.txtRoom.TabIndex = 10;
            this.txtRoom.Text = "";
            // 
            // lblWelcome
            // 
            this.lblWelcome.BackColor = System.Drawing.Color.Transparent;
            this.lblWelcome.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWelcome.Location = new System.Drawing.Point(56, 20);
            this.lblWelcome.Name = "lblWelcome";
            this.lblWelcome.Size = new System.Drawing.Size(328, 16);
            this.lblWelcome.TabIndex = 11;
            this.lblWelcome.Text = "Welcome";
            // 
            // btnSignout
            // 
            this.btnSignout.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnSignout.BackgroundImage")));
            this.btnSignout.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSignout.Location = new System.Drawing.Point(464, 16);
            this.btnSignout.Name = "btnSignout";
            this.btnSignout.Size = new System.Drawing.Size(82, 30);
            this.btnSignout.TabIndex = 12;
            this.btnSignout.Click += new System.EventHandler(this.OnClickSignout);
            // 
            // RoomForm
            // 
            this.AcceptButton = this.btnSend;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.BackColor = System.Drawing.SystemColors.Window;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.ClientSize = new System.Drawing.Size(554, 559);
            this.Controls.Add(this.btnSignout);
            this.Controls.Add(this.lblWelcome);
            this.Controls.Add(this.txtRoom);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtMsg);
            this.Controls.Add(this.listUsers);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "RoomForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "NCache ChatRoom";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.OnRoomFormClosing);
            this.Load += new System.EventHandler(this.OnLoadRoomForm);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		#region	/                 --- Room Notification Handler ---           /

        private delegate void OnChatRoomEventHandler(Msg.Code code, object msg);
        
        /// <summary>
        /// Hanlder for application when some an custom event occours.
        /// </summary>
        /// <param name="opcode"></param>
        /// <param name="operand"></param>
        private void OnAppEvent(object opcode, object operand)
        {
            Msg.Code code = (Msg.Code)opcode;
            this.Invoke(new OnChatRoomEventHandler(this.OnChatRoomEvent),
                new object[] { code, operand });
        }

        /// <summary>
        /// Hanlder for application when some an custom event occours.
        /// </summary>
        /// <param name="opcode"></param>
        /// <param name="operand"></param>
        private void OnChatRoomEvent(Msg.Code opcode, object operand)
        {
            switch (opcode)
            {
            case Msg.Code.NewUser:
                string userName = operand as string;
                if (!listUsers.Items.Contains(userName))
                {
                    listUsers.Items.Add(userName);
                    txtRoom.SelectionStart = txtRoom.Text.Length + 1;
                    txtRoom.SelectionColor = Color.SteelBlue;
                    txtRoom.SelectionFont = new Font("Tahoma", 8, FontStyle.Italic);
                    txtRoom.SelectionIndent = 0;
                    txtRoom.AppendText(@"'" + userName + "' has joined the conversation.\r\n");
                }
                break;

            case Msg.Code.UserLeft:
                userName = operand as string;
                if (listUsers.Items.Contains(userName)) // if user exist in list
                {
                    listUsers.Items.Remove(userName);
                    txtRoom.SelectionStart = txtRoom.Text.Length + 1;
                    txtRoom.SelectionColor = Color.SteelBlue;
                    txtRoom.SelectionFont = new Font("Tahoma", 8, FontStyle.Italic);
                    txtRoom.SelectionIndent = 0;
                    txtRoom.AppendText(@"'" + userName + "' has left the conversation.\r\n");
                }                
                break;

            case Msg.Code.Text:
                {
                    Msg msg = Helper.FromByteBuffer(operand as byte[]) as Msg;
                    if (msg == null) return; /// not a message maybe
                    if ((msg.To == null))
                    {
                        txtRoom.SelectionStart = txtRoom.Text.Length + 1;
                        if (msg.From.CompareTo(_userName) == 0)
                            txtRoom.SelectionColor = Color.SteelBlue;
                        else
                            txtRoom.SelectionColor = Color.ForestGreen;
                        txtRoom.SelectionFont = new Font("Tahoma", 9, FontStyle.Bold);
                        txtRoom.SelectionIndent = 0;
                        txtRoom.AppendText(msg.From + " says: \r\n");

                        txtRoom.SelectionStart = txtRoom.Text.Length + 1;
                        txtRoom.SelectionColor = Color.Black;
                        txtRoom.SelectionFont = new Font("Tahoma", 9);
                        txtRoom.SelectionIndent = 10;
                        txtRoom.AppendText(msg.Data.ToString() + "\r\n");
                    }
                }
                break;
            }
            txtRoom.ScrollToCaret();
        }
            
		#endregion
        
		#region	/                 --- RoomForm Message Hnalders ---           /

		/// <summary>
		/// Called when the form is loaded.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnLoadRoomForm(object sender, System.EventArgs e)
		{
			lblWelcome.Text = "Welcome, " + _userName + "!"; 
			
			/// Subscribe to custom notifications, so that we can listen to room events.
            _appEvent = new CustomEventCallback(this.OnAppEvent);
            NCache.Web.Caching.NCache.Caches[Helper.CacheName].CustomEvent += new CustomEventCallback(this.OnAppEvent);

			PopulateUsersList();
		}

		/// <summary>
		/// Called when the form is closing.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnRoomFormClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			try
			{
                ///unregister the custom event handler.
                NCache.Web.Caching.NCache.Caches[Helper.CacheName].CustomEvent -= _appEvent;
                _appEvent = null;
               ///Notifiy that an existing user has left the conversation.
                NCache.Web.Caching.NCache.Caches[Helper.CacheName].RaiseCustomEvent(Msg.Code.UserLeft, _userName);
                /// Remove our user key from the cache.
                NCache.Web.Caching.NCache.Caches[Helper.CacheName].Remove("<user>" + _userName);
			}
			catch(Exception)
			{
			}
		}

		/// <summary>
		/// Called when the send button is clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnClickSend(object sender, System.EventArgs e)
		{
			Msg msg = new Msg(txtMsg.Text,Msg.Code.NewUser, _userName, null);
			try
			{
                ///Send a custom notification, along with the message. On identifying the code
                ///notification handler will take the appropriate action, i.e the message will
                ///be shown in the message box.
                NCache.Web.Caching.NCache.Caches[Helper.CacheName].RaiseCustomEvent(Msg.Code.Text,
                    Helper.ToByteBuffer(msg));

				txtMsg.Text = "";
                txtMsg.Focus();
			}
			catch(Exception ex)
			{
				MessageBox.Show(this,
					"Error occured while trying to send message, " + ex.ToString(),
					Program.Title, 
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
		}

		private void OnClickSignout(object sender, System.EventArgs e)
		{
			this.Close();         
		}

		#endregion

		/// <summary>
		/// Finds and list all the users in the chat room.
		/// </summary>
		private void PopulateUsersList()
		{
			listUsers.Items.Clear();
			try
			{
                IDictionaryEnumerator ide = (IDictionaryEnumerator)NCache.Web.Caching.NCache.Caches[Helper.CacheName].GetEnumerator();
				while(ide.MoveNext())
				{
					string k = ide.Key as string;
					if(k != null)
					{
						/// check if its a user key!
						if(k.IndexOf("<user>") == 0)
							listUsers.Items.Add(k.Substring(6)); 
					}
				}
			}
			catch(Exception e)
			{
				MessageBox.Show(this,
					"Error occured while scanning for users" + e.ToString(),
					Program.Title, 
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
		}
	}
}