namespace Lextm.SharpSnmpLib.Browser
{
    partial class FormSet
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
            this.txtCurrent = new System.Windows.Forms.TextBox();
            this.txtNew = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.rbString = new System.Windows.Forms.RadioButton();
            this.rbInteger = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // txtCurrent
            // 
            this.txtCurrent.Location = new System.Drawing.Point(12, 25);
            this.txtCurrent.Multiline = true;
            this.txtCurrent.Name = "txtCurrent";
            this.txtCurrent.Size = new System.Drawing.Size(268, 75);
            this.txtCurrent.TabIndex = 0;
            // 
            // txtNew
            // 
            this.txtNew.Location = new System.Drawing.Point(12, 132);
            this.txtNew.Multiline = true;
            this.txtNew.Name = "txtNew";
            this.txtNew.Size = new System.Drawing.Size(268, 75);
            this.txtNew.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Current Value:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 116);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "New Value:";
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(33, 283);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "Ok";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOkClick);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(205, 283);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancelClick);
            // 
            // rbString
            // 
            this.rbString.AutoSize = true;
            this.rbString.Location = new System.Drawing.Point(15, 214);
            this.rbString.Name = "rbString";
            this.rbString.Size = new System.Drawing.Size(52, 17);
            this.rbString.TabIndex = 6;
            this.rbString.TabStop = true;
            this.rbString.Text = "String";
            this.rbString.UseVisualStyleBackColor = true;
            // 
            // rbInteger
            // 
            this.rbInteger.AutoSize = true;
            this.rbInteger.Location = new System.Drawing.Point(15, 237);
            this.rbInteger.Name = "rbInteger";
            this.rbInteger.Size = new System.Drawing.Size(58, 17);
            this.rbInteger.TabIndex = 7;
            this.rbInteger.TabStop = true;
            this.rbInteger.Text = "Integer";
            this.rbInteger.UseVisualStyleBackColor = true;
            this.rbInteger.CheckedChanged += new System.EventHandler(this.RbIntegerCheckedChanged);
            // 
            // FormSet
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(289, 313);
            this.Controls.Add(this.rbInteger);
            this.Controls.Add(this.rbString);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtNew);
            this.Controls.Add(this.txtCurrent);
            this.Name = "FormSet";
            this.Text = "Set";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtCurrent;
        private System.Windows.Forms.TextBox txtNew;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.RadioButton rbString;
        private System.Windows.Forms.RadioButton rbInteger;
    }
}