/*
 * Created by SharpDevelop.
 * User: lexli
 * Date: 2008-12-14
 * Time: 14:08
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

namespace Lextm.SharpSnmpLib.Agent
{
    partial class MainForm
    {
        /// <summary>
        /// Designer variable used to keep track of non-visual components.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        
        /// <summary>
        /// Disposes resources used by the form.
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
            this.btnTrap = new System.Windows.Forms.Button();
            this.btnTrap2 = new System.Windows.Forms.Button();
            this.txtIP = new System.Windows.Forms.TextBox();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnInformV2 = new System.Windows.Forms.Button();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.tscbIP = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.tstxtPort = new System.Windows.Forms.ToolStripTextBox();
            this.actEnabled = new Crad.Windows.Forms.Actions.Action();
            this.alNotification = new Crad.Windows.Forms.Actions.ActionList();
            this.btnInformV3 = new System.Windows.Forms.Button();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.alNotification)).BeginInit();
            this.SuspendLayout();
            // 
            // btnTrap
            // 
            this.btnTrap.Location = new System.Drawing.Point(26, 106);
            this.btnTrap.Name = "btnTrap";
            this.btnTrap.Size = new System.Drawing.Size(93, 23);
            this.btnTrap.TabIndex = 2;
            this.btnTrap.Text = "Send Trap v1";
            this.btnTrap.UseVisualStyleBackColor = true;
            this.btnTrap.Click += new System.EventHandler(this.BtnTrapClick);
            // 
            // btnTrap2
            // 
            this.btnTrap2.Location = new System.Drawing.Point(125, 106);
            this.btnTrap2.Name = "btnTrap2";
            this.btnTrap2.Size = new System.Drawing.Size(93, 23);
            this.btnTrap2.TabIndex = 3;
            this.btnTrap2.Text = "Send Trap v2";
            this.btnTrap2.UseVisualStyleBackColor = true;
            this.btnTrap2.Click += new System.EventHandler(this.BtnTrap2Click);
            // 
            // txtIP
            // 
            this.txtIP.Location = new System.Drawing.Point(84, 32);
            this.txtIP.Name = "txtIP";
            this.txtIP.Size = new System.Drawing.Size(100, 20);
            this.txtIP.TabIndex = 4;
            this.txtIP.Text = "127.0.0.1";
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(84, 58);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(49, 20);
            this.txtPort.TabIndex = 5;
            this.txtPort.Text = "162";
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 23);
            this.label1.TabIndex = 6;
            this.label1.Text = "Manager IP";
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(40, 61);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 23);
            this.label2.TabIndex = 7;
            this.label2.Text = "Port";
            // 
            // btnInformV2
            // 
            this.btnInformV2.Location = new System.Drawing.Point(224, 106);
            this.btnInformV2.Name = "btnInformV2";
            this.btnInformV2.Size = new System.Drawing.Size(93, 23);
            this.btnInformV2.TabIndex = 8;
            this.btnInformV2.Text = "Send Inform v2";
            this.btnInformV2.UseVisualStyleBackColor = true;
            this.btnInformV2.Click += new System.EventHandler(this.BtnInformV2Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton1,
            this.toolStripSeparator1,
            this.toolStripLabel1,
            this.tscbIP,
            this.toolStripLabel2,
            this.tstxtPort});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(353, 25);
            this.toolStrip1.TabIndex = 9;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButton1
            // 
            this.alNotification.SetAction(this.toolStripButton1, this.actEnabled);
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(77, 22);
            this.toolStripButton1.Text = "Start listening";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(17, 22);
            this.toolStripLabel1.Text = "IP";
            // 
            // tscbIP
            // 
            this.tscbIP.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tscbIP.Name = "tscbIP";
            this.tscbIP.Size = new System.Drawing.Size(150, 25);
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(27, 22);
            this.toolStripLabel2.Text = "Port";
            // 
            // tstxtPort
            // 
            this.tstxtPort.Name = "tstxtPort";
            this.tstxtPort.Size = new System.Drawing.Size(40, 25);
            // 
            // actEnabled
            // 
            this.actEnabled.Text = "Start listening";
            this.actEnabled.ToolTipText = "Control listening";
            this.actEnabled.Execute += new System.EventHandler(this.ActEnabledExecute);
            this.actEnabled.AfterExecute += new System.EventHandler(this.ActEnabledAfterExecute);
            // 
            // alNotification
            // 
            this.alNotification.Actions.Add(this.actEnabled);
            this.alNotification.ContainerControl = this;
            // 
            // btnInformV3
            // 
            this.btnInformV3.Location = new System.Drawing.Point(224, 134);
            this.btnInformV3.Name = "btnInformV3";
            this.btnInformV3.Size = new System.Drawing.Size(93, 23);
            this.btnInformV3.TabIndex = 10;
            this.btnInformV3.Text = "Send Inform v3";
            this.btnInformV3.UseVisualStyleBackColor = true;
            this.btnInformV3.Click += new System.EventHandler(this.BtnInformV3Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(353, 169);
            this.Controls.Add(this.btnInformV3);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.btnInformV2);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtPort);
            this.Controls.Add(this.txtIP);
            this.Controls.Add(this.btnTrap2);
            this.Controls.Add(this.btnTrap);
            this.Name = "MainForm";
            this.Text = "#SNMP Agent";
            this.Load += new System.EventHandler(this.MainFormLoad);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.alNotification)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        private System.Windows.Forms.Button btnInformV2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.TextBox txtIP;
        private System.Windows.Forms.Button btnTrap2;
        private System.Windows.Forms.Button btnTrap;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripComboBox tscbIP;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripTextBox tstxtPort;
        private Crad.Windows.Forms.Actions.ActionList alNotification;
        private Crad.Windows.Forms.Actions.Action actEnabled;
        private System.Windows.Forms.Button btnInformV3;

    }
}
