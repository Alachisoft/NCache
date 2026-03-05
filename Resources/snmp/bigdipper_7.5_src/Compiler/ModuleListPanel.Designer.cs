/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/7/20
 * Time: 20:32
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
namespace Lextm.SharpSnmpLib.Compiler
{
    partial class ModuleListPanel
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
        	System.Windows.Forms.ListViewGroup listViewGroup1 = new System.Windows.Forms.ListViewGroup("Loaded", System.Windows.Forms.HorizontalAlignment.Left);
        	System.Windows.Forms.ListViewGroup listViewGroup2 = new System.Windows.Forms.ListViewGroup("Pending", System.Windows.Forms.HorizontalAlignment.Left);
        	this.listView1 = new System.Windows.Forms.ListView();
        	this.chName = new System.Windows.Forms.ColumnHeader();
        	this.actionList1 = new Crad.Windows.Forms.Actions.ActionList();
        	this.actRemove = new Crad.Windows.Forms.Actions.Action();
        	this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        	this.contextModuleMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
        	this.statusStrip1 = new System.Windows.Forms.StatusStrip();
        	this.tslblCount = new System.Windows.Forms.ToolStripStatusLabel();
        	((System.ComponentModel.ISupportInitialize)(this.actionList1)).BeginInit();
        	this.contextModuleMenu.SuspendLayout();
        	this.statusStrip1.SuspendLayout();
        	this.SuspendLayout();
        	// 
        	// listView1
        	// 
        	this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
        	        	        	this.chName});
        	this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
        	listViewGroup1.Header = "Loaded";
        	listViewGroup1.Name = "lvgLoaded";
        	listViewGroup2.Header = "Pending";
        	listViewGroup2.Name = "lvgPending";
        	this.listView1.Groups.AddRange(new System.Windows.Forms.ListViewGroup[] {
        	        	        	listViewGroup1,
        	        	        	listViewGroup2});
        	this.listView1.Location = new System.Drawing.Point(0, 0);
        	this.listView1.MultiSelect = false;
        	this.listView1.Name = "listView1";
        	this.listView1.Size = new System.Drawing.Size(465, 277);
        	this.listView1.TabIndex = 0;
        	this.listView1.UseCompatibleStateImageBehavior = false;
        	this.listView1.View = System.Windows.Forms.View.Details;
        	this.listView1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ListView1MouseDown);
        	// 
        	// chName
        	// 
        	this.chName.Text = "Name";
        	this.chName.Width = 381;
        	// 
        	// actionList1
        	// 
        	this.actionList1.Actions.Add(this.actRemove);
        	this.actionList1.ContainerControl = this;
        	// 
        	// actRemove
        	// 
        	this.actRemove.Text = "Remove";
        	this.actRemove.ToolTipText = "Remove";
        	this.actRemove.Update += new System.EventHandler(this.ActRemoveUpdate);
        	this.actRemove.Execute += new System.EventHandler(this.ActRemoveExecute);
        	// 
        	// removeToolStripMenuItem
        	// 
        	this.actionList1.SetAction(this.removeToolStripMenuItem, this.actRemove);
        	this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
        	this.removeToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
        	this.removeToolStripMenuItem.Text = "Remove";
        	// 
        	// contextModuleMenu
        	// 
        	this.contextModuleMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
        	        	        	this.removeToolStripMenuItem});
        	this.contextModuleMenu.Name = "contextModuleMenu";
        	this.contextModuleMenu.Size = new System.Drawing.Size(118, 26);
        	// 
        	// statusStrip1
        	// 
        	this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
        	        	        	this.tslblCount});
        	this.statusStrip1.Location = new System.Drawing.Point(0, 277);
        	this.statusStrip1.Name = "statusStrip1";
        	this.statusStrip1.Size = new System.Drawing.Size(465, 22);
        	this.statusStrip1.TabIndex = 1;
        	this.statusStrip1.Text = "statusStrip1";
        	// 
        	// tslblCount
        	// 
        	this.tslblCount.Name = "tslblCount";
        	this.tslblCount.Size = new System.Drawing.Size(10, 17);
        	this.tslblCount.Text = " ";
        	// 
        	// ModuleListPanel
        	// 
        	this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        	this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        	this.ClientSize = new System.Drawing.Size(465, 299);
        	this.Controls.Add(this.listView1);
        	this.Controls.Add(this.statusStrip1);        	
        	this.Name = "ModuleListPanel";
        	this.TabText = "Module List";
        	this.Load += new System.EventHandler(this.ModuleListPanelLoad);
        	((System.ComponentModel.ISupportInitialize)(this.actionList1)).EndInit();
        	this.contextModuleMenu.ResumeLayout(false);
        	this.statusStrip1.ResumeLayout(false);
        	this.statusStrip1.PerformLayout();
        	this.ResumeLayout(false);
        	this.PerformLayout();
        }
        private System.Windows.Forms.ListView listView1;
        private Crad.Windows.Forms.Actions.ActionList actionList1;
        private System.Windows.Forms.ColumnHeader chName;
        private System.Windows.Forms.ContextMenuStrip contextModuleMenu;
        private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tslblCount;
        private Crad.Windows.Forms.Actions.Action actRemove;
    }
}
