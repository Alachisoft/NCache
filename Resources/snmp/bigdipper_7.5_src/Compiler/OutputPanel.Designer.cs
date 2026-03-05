/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/6/28
 * Time: 15:33
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
namespace Lextm.SharpSnmpLib.Compiler
{
	partial class OutputPanel
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the control.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.txtMessages = new System.Windows.Forms.RichTextBox();
			this.contextOuputMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.actionList1 = new Crad.Windows.Forms.Actions.ActionList();
			this.actClear = new Crad.Windows.Forms.Actions.Action();
			this.contextOuputMenu.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.actionList1)).BeginInit();
			this.SuspendLayout();
			// 
			// txtMessages
			// 
			this.txtMessages.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtMessages.Location = new System.Drawing.Point(0, 0);
			this.txtMessages.Name = "txtMessages";
			this.txtMessages.Size = new System.Drawing.Size(511, 154);
			this.txtMessages.TabIndex = 0;
			this.txtMessages.Text = "";
			this.txtMessages.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TxtMessagesMouseUp);
			// 
			// contextOuputMenu
			// 
			this.contextOuputMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.clearToolStripMenuItem});
			this.contextOuputMenu.Name = "contextOuputMenu";
			this.contextOuputMenu.Size = new System.Drawing.Size(102, 26);
			// 
			// clearToolStripMenuItem
			// 
			this.actionList1.SetAction(this.clearToolStripMenuItem, this.actClear);
			this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
			this.clearToolStripMenuItem.Size = new System.Drawing.Size(101, 22);
			this.clearToolStripMenuItem.Text = "Clear";
			// 
			// actionList1
			// 
			this.actionList1.Actions.Add(this.actClear);
			this.actionList1.ContainerControl = this;
			// 
			// actClear
			// 
			this.actClear.Text = "Clear";
			this.actClear.ToolTipText = "Clear Panel";
			this.actClear.Execute += new System.EventHandler(this.ActClearExecute);
			// 
			// OutputPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(511, 154);
			this.Controls.Add(this.txtMessages);			
			this.Name = "OutputPanel";
			this.TabText = "Output";
			this.contextOuputMenu.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.actionList1)).EndInit();
			this.ResumeLayout(false);
		}
		private System.Windows.Forms.RichTextBox txtMessages;
        private System.Windows.Forms.ContextMenuStrip contextOuputMenu;
        private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
        private Crad.Windows.Forms.Actions.ActionList actionList1;
        private Crad.Windows.Forms.Actions.Action actClear;
	}
}
