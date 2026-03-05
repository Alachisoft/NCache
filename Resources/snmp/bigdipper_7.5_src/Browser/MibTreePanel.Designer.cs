/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/6/28
 * Time: 15:25
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
namespace Lextm.SharpSnmpLib.Browser
{
	partial class MibTreePanel
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MibTreePanel));
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.getToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.getNextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.walkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.getTableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyNumericOIDToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyTextualOIDToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.actionList1 = new Crad.Windows.Forms.Actions.ActionList();
            this.actGet = new Crad.Windows.Forms.Actions.Action();
            this.actGetNext = new Crad.Windows.Forms.Actions.Action();
            this.actSet = new Crad.Windows.Forms.Actions.Action();
            this.actWalk = new Crad.Windows.Forms.Actions.Action();
            this.actGetTable = new Crad.Windows.Forms.Actions.Action();
            this.actGetNumeric = new Crad.Windows.Forms.Actions.Action();
            this.actGetTextual = new Crad.Windows.Forms.Actions.Action();
            this.actNumber = new Crad.Windows.Forms.Actions.Action();
            this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tslblOID = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.actionList1)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // treeView1
            // 
            this.treeView1.ContextMenuStrip = this.contextMenuStrip1;
            this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView1.HideSelection = false;
            this.treeView1.ImageIndex = 0;
            this.treeView1.ImageList = this.imageList1;
            this.treeView1.Location = new System.Drawing.Point(0, 25);
            this.treeView1.Name = "treeView1";
            this.treeView1.SelectedImageIndex = 0;
            this.treeView1.ShowNodeToolTips = true;
            this.treeView1.Size = new System.Drawing.Size(284, 239);
            this.treeView1.TabIndex = 0;
            this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TreeView1AfterSelect);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.getToolStripMenuItem,
            this.getNextToolStripMenuItem,
            this.setToolStripMenuItem,
            this.walkToolStripMenuItem,
            this.getTableToolStripMenuItem,
            this.copyNumericOIDToolStripMenuItem,
            this.copyTextualOIDToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(167, 158);
            // 
            // getToolStripMenuItem
            // 
            this.actionList1.SetAction(this.getToolStripMenuItem, this.actGet);
            this.getToolStripMenuItem.Name = "getToolStripMenuItem";
            this.getToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.getToolStripMenuItem.Text = "Get";
            // 
            // getNextToolStripMenuItem
            // 
            this.actionList1.SetAction(this.getNextToolStripMenuItem, this.actGetNext);
            this.getNextToolStripMenuItem.Name = "getNextToolStripMenuItem";
            this.getNextToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.getNextToolStripMenuItem.Text = "Get Next";
            // 
            // setToolStripMenuItem
            // 
            this.actionList1.SetAction(this.setToolStripMenuItem, this.actSet);
            this.setToolStripMenuItem.Name = "setToolStripMenuItem";
            this.setToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.setToolStripMenuItem.Text = "Set";
            // 
            // walkToolStripMenuItem
            // 
            this.actionList1.SetAction(this.walkToolStripMenuItem, this.actWalk);
            this.walkToolStripMenuItem.Name = "walkToolStripMenuItem";
            this.walkToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.walkToolStripMenuItem.Text = "Walk";
            // 
            // getTableToolStripMenuItem
            // 
            this.actionList1.SetAction(this.getTableToolStripMenuItem, this.actGetTable);
            this.getTableToolStripMenuItem.Name = "getTableToolStripMenuItem";
            this.getTableToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.getTableToolStripMenuItem.Text = "Get Table";
            // 
            // copyNumericOIDToolStripMenuItem
            // 
            this.actionList1.SetAction(this.copyNumericOIDToolStripMenuItem, this.actGetNumeric);
            this.copyNumericOIDToolStripMenuItem.Name = "copyNumericOIDToolStripMenuItem";
            this.copyNumericOIDToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.copyNumericOIDToolStripMenuItem.Text = "Copy Numeric OID";
            // 
            // copyTextualOIDToolStripMenuItem
            // 
            this.actionList1.SetAction(this.copyTextualOIDToolStripMenuItem, this.actGetTextual);
            this.copyTextualOIDToolStripMenuItem.Name = "copyTextualOIDToolStripMenuItem";
            this.copyTextualOIDToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.copyTextualOIDToolStripMenuItem.Text = "Copy Textual OID";
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "text-x-generic-template.png");
            this.imageList1.Images.SetKeyName(1, "package-x-generic.png");
            this.imageList1.Images.SetKeyName(2, "text-x-generic.png");
            this.imageList1.Images.SetKeyName(3, "x-office-spreadsheet.png");
            this.imageList1.Images.SetKeyName(4, "application-x-executable.png");
            this.imageList1.Images.SetKeyName(5, "text-x-script.png");
            // 
            // actionList1
            // 
            this.actionList1.Actions.Add(this.actGet);
            this.actionList1.Actions.Add(this.actSet);
            this.actionList1.Actions.Add(this.actWalk);
            this.actionList1.Actions.Add(this.actGetTable);
            this.actionList1.Actions.Add(this.actGetNext);
            this.actionList1.Actions.Add(this.actNumber);
            this.actionList1.Actions.Add(this.actGetNumeric);
            this.actionList1.Actions.Add(this.actGetTextual);
            this.actionList1.ContainerControl = this;
            // 
            // actGet
            // 
            this.actGet.Text = "Get";
            this.actGet.ToolTipText = "Get";
            this.actGet.Execute += new System.EventHandler(this.ActGetExecute);
            this.actGet.Update += new System.EventHandler(this.ActGetUpdate);
            // 
            // actGetNext
            // 
            this.actGetNext.Text = "Get Next";
            this.actGetNext.ToolTipText = "Get Next";
            this.actGetNext.Execute += new System.EventHandler(this.ActGetNextExecute);
            this.actGetNext.Update += new System.EventHandler(this.ActGetNextUpdate);
            // 
            // actSet
            // 
            this.actSet.Text = "Set";
            this.actSet.ToolTipText = "Set";
            this.actSet.Execute += new System.EventHandler(this.ActSetExecute);
            this.actSet.Update += new System.EventHandler(this.ActSetUpdate);
            // 
            // actWalk
            // 
            this.actWalk.Text = "Walk";
            this.actWalk.ToolTipText = "Walk";
            this.actWalk.Execute += new System.EventHandler(this.ActWalkExecute);
            this.actWalk.Update += new System.EventHandler(this.ActWalkUpdate);
            // 
            // actGetTable
            // 
            this.actGetTable.Text = "Get Table";
            this.actGetTable.ToolTipText = "Get Table";
            this.actGetTable.Execute += new System.EventHandler(this.ActGetTableExecute);
            this.actGetTable.Update += new System.EventHandler(this.ActGetTableUpdate);
            // 
            // actGetNumeric
            // 
            this.actGetNumeric.Text = "Copy Numeric OID";
            this.actGetNumeric.Execute += new System.EventHandler(this.ActGetNumericExecute);
            this.actGetNumeric.Update += new System.EventHandler(this.ActGetNumericUpdate);
            // 
            // actGetTextual
            // 
            this.actGetTextual.Text = "Copy Textual OID";
            this.actGetTextual.Execute += new System.EventHandler(this.ActGetTextualExecute);
            this.actGetTextual.Update += new System.EventHandler(this.ActGetNumericUpdate);
            // 
            // actNumber
            // 
            this.actNumber.CheckOnClick = true;
            this.actNumber.Text = "Show ID";
            this.actNumber.ToolTipText = "Show object ID";
            this.actNumber.Execute += new System.EventHandler(this.ActNumberExecute);
            // 
            // toolStripButton2
            // 
            this.actionList1.SetAction(this.toolStripButton2, this.actNumber);
            this.toolStripButton2.CheckOnClick = true;
            this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton2.Name = "toolStripButton2";
            this.toolStripButton2.Size = new System.Drawing.Size(51, 22);
            this.toolStripButton2.Text = "Show ID";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tslblOID});
            this.statusStrip1.Location = new System.Drawing.Point(0, 264);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(284, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tslblOID
            // 
            this.tslblOID.Name = "tslblOID";
            this.tslblOID.Size = new System.Drawing.Size(11, 17);
            this.tslblOID.Text = " ";
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton2});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(284, 25);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // MibTreePanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 286);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.statusStrip1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "MibTreePanel";
            this.Load += new System.EventHandler(this.MibTreePanelLoad);
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.actionList1)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
		private System.Windows.Forms.ToolStripMenuItem copyTextualOIDToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copyNumericOIDToolStripMenuItem;
		private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem getToolStripMenuItem;
        private Crad.Windows.Forms.Actions.ActionList actionList1;
        private Crad.Windows.Forms.Actions.Action actGet;
        private Crad.Windows.Forms.Actions.Action actSet;
        private System.Windows.Forms.ToolStripMenuItem setToolStripMenuItem;
        private System.Windows.Forms.ImageList imageList1;
        private Crad.Windows.Forms.Actions.Action actWalk;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tslblOID;
        private System.Windows.Forms.ToolStripMenuItem walkToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem getNextToolStripMenuItem;
        private Crad.Windows.Forms.Actions.Action actGetNext;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private Crad.Windows.Forms.Actions.Action actNumber;
        private System.Windows.Forms.ToolStripButton toolStripButton2;
        private Crad.Windows.Forms.Actions.Action actGetTable;
        private System.Windows.Forms.ToolStripMenuItem getTableToolStripMenuItem;
        private Crad.Windows.Forms.Actions.Action actGetNumeric;
        private Crad.Windows.Forms.Actions.Action actGetTextual;
	}
}
