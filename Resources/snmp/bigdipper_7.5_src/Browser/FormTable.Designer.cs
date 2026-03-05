namespace Lextm.SharpSnmpLib.Browser
{
    partial class FormTable
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
        	System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
        	System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
        	System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
        	this.dataGridTable = new System.Windows.Forms.DataGridView();
        	this.checkBoxRefresh = new System.Windows.Forms.CheckBox();
        	this.label1 = new System.Windows.Forms.Label();
        	this.cbColumnDisplay = new System.Windows.Forms.ComboBox();
        	this.textBoxRefresh = new System.Windows.Forms.TextBox();
        	((System.ComponentModel.ISupportInitialize)(this.dataGridTable)).BeginInit();
        	this.SuspendLayout();
        	// 
        	// dataGridTable
        	// 
        	this.dataGridTable.AllowUserToAddRows = false;
        	this.dataGridTable.AllowUserToDeleteRows = false;
        	this.dataGridTable.AllowUserToOrderColumns = true;
        	this.dataGridTable.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
        	        	        	| System.Windows.Forms.AnchorStyles.Left) 
        	        	        	| System.Windows.Forms.AnchorStyles.Right)));
        	dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
        	dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
        	dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        	dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
        	dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
        	dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
        	dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
        	this.dataGridTable.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
        	this.dataGridTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        	dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
        	dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
        	dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        	dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
        	dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
        	dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
        	dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
        	this.dataGridTable.DefaultCellStyle = dataGridViewCellStyle2;
        	this.dataGridTable.Location = new System.Drawing.Point(12, 12);
        	this.dataGridTable.MultiSelect = false;
        	this.dataGridTable.Name = "dataGridTable";
        	dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
        	dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
        	dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        	dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
        	dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
        	dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
        	dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
        	this.dataGridTable.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
        	this.dataGridTable.Size = new System.Drawing.Size(448, 381);
        	this.dataGridTable.TabIndex = 0;
        	// 
        	// checkBoxRefresh
        	// 
        	this.checkBoxRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
        	this.checkBoxRefresh.AutoSize = true;
        	this.checkBoxRefresh.Location = new System.Drawing.Point(13, 399);
        	this.checkBoxRefresh.Name = "checkBoxRefresh";
        	this.checkBoxRefresh.Size = new System.Drawing.Size(93, 17);
        	this.checkBoxRefresh.TabIndex = 1;
        	this.checkBoxRefresh.Text = "Refresh Table";
        	this.checkBoxRefresh.UseVisualStyleBackColor = true;
        	this.checkBoxRefresh.CheckedChanged += new System.EventHandler(this.CheckBoxRefreshCheckedChanged);
        	// 
        	// label1
        	// 
        	this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
        	this.label1.AutoSize = true;
        	this.label1.Location = new System.Drawing.Point(159, 400);
        	this.label1.Name = "label1";
        	this.label1.Size = new System.Drawing.Size(49, 13);
        	this.label1.TabIndex = 3;
        	this.label1.Text = "Seconds";
        	// 
        	// cbColumnDisplay
        	// 
        	this.cbColumnDisplay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        	this.cbColumnDisplay.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        	this.cbColumnDisplay.FormattingEnabled = true;
        	this.cbColumnDisplay.Items.AddRange(new object[] {
        	        	        	"Full Text",
        	        	        	"Name"});
        	this.cbColumnDisplay.Location = new System.Drawing.Point(365, 397);
        	this.cbColumnDisplay.Name = "cbColumnDisplay";
        	this.cbColumnDisplay.Size = new System.Drawing.Size(95, 21);
        	this.cbColumnDisplay.TabIndex = 4;
        	this.cbColumnDisplay.SelectedIndexChanged += new System.EventHandler(this.CbColumnDisplaySelectedIndexChanged);
        	// 
        	// textBoxRefresh
        	// 
        	this.textBoxRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
        	this.textBoxRefresh.Location = new System.Drawing.Point(214, 397);
        	this.textBoxRefresh.MaxLength = 2;
        	this.textBoxRefresh.Name = "textBoxRefresh";
        	this.textBoxRefresh.Size = new System.Drawing.Size(67, 20);
        	this.textBoxRefresh.TabIndex = 2;
        	this.textBoxRefresh.Text = "10";
        	this.textBoxRefresh.WordWrap = false;
        	this.textBoxRefresh.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBoxRefreshKeyDown);
        	// 
        	// FormTable
        	// 
        	this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        	this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        	this.ClientSize = new System.Drawing.Size(472, 423);
        	this.Controls.Add(this.cbColumnDisplay);
        	this.Controls.Add(this.label1);
        	this.Controls.Add(this.textBoxRefresh);
        	this.Controls.Add(this.checkBoxRefresh);
        	this.Controls.Add(this.dataGridTable);
        	this.MinimumSize = new System.Drawing.Size(440, 400);
        	this.Name = "FormTable";
        	this.Text = "FormTable";
        	this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormTableFormClosing);
        	((System.ComponentModel.ISupportInitialize)(this.dataGridTable)).EndInit();
        	this.ResumeLayout(false);
        	this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridTable;
        private System.Windows.Forms.CheckBox checkBoxRefresh;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbColumnDisplay;
        private System.Windows.Forms.TextBox textBoxRefresh;

    }
}