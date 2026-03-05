/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2009/1/28
 * Time: 18:00
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
namespace Lextm.SharpSnmpLib.Compiler
{
	partial class DocumentListPanel
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
			this.lvFiles = new System.Windows.Forms.ListView();
			this.chFileName = new System.Windows.Forms.ColumnHeader();
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.actionList1 = new Crad.Windows.Forms.Actions.ActionList();
			this.actDelete = new Crad.Windows.Forms.Actions.Action();
			this.contextMenuStrip1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.actionList1)).BeginInit();
			this.SuspendLayout();
			// 
			// lvFiles
			// 
			this.lvFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
									this.chFileName});
			this.lvFiles.ContextMenuStrip = this.contextMenuStrip1;
			this.lvFiles.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lvFiles.FullRowSelect = true;
			this.lvFiles.Location = new System.Drawing.Point(0, 0);
			this.lvFiles.Name = "lvFiles";
			this.lvFiles.Size = new System.Drawing.Size(284, 262);
			this.lvFiles.TabIndex = 0;
			this.lvFiles.UseCompatibleStateImageBehavior = false;
			this.lvFiles.View = System.Windows.Forms.View.Details;
			this.lvFiles.DoubleClick += new System.EventHandler(this.LvFilesDoubleClick);
			// 
			// chFileName
			// 
			this.chFileName.Text = "Filename";
			this.chFileName.Width = 274;
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.deleteToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(108, 26);
			// 
			// deleteToolStripMenuItem
			// 
			this.actionList1.SetAction(this.deleteToolStripMenuItem, this.actDelete);
			this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
			this.deleteToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
			this.deleteToolStripMenuItem.Text = "Delete";
			// 
			// actionList1
			// 
			this.actionList1.Actions.Add(this.actDelete);
			this.actionList1.ContainerControl = this;
			// 
			// actDelete
			// 
			this.actDelete.Text = "Delete";
			this.actDelete.ToolTipText = "Delete";
			this.actDelete.Update += new System.EventHandler(this.ActDeleteUpdate);
			this.actDelete.Execute += new System.EventHandler(this.ActDeleteExecute);
			// 
			// DocumentListPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.Controls.Add(this.lvFiles);
			this.Name = "DocumentListPanel";
			this.TabText = "Document List";
			this.Load += new System.EventHandler(this.DocumentListPanelLoad);
			this.contextMenuStrip1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.actionList1)).EndInit();
			this.ResumeLayout(false);
		}
		private Crad.Windows.Forms.Actions.ActionList actionList1;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ListView lvFiles;
        private System.Windows.Forms.ColumnHeader chFileName;
        private Crad.Windows.Forms.Actions.Action actDelete;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
	}
}
