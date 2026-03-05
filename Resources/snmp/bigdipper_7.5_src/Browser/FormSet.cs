using System;
using System.Windows.Forms;

namespace Lextm.SharpSnmpLib.Browser
{
    internal partial class FormSet : Form
    {
        public FormSet()
        {
            InitializeComponent();

            rbString.Checked = true;
            txtCurrent.Enabled = false;
        }

        public string OldVal
        {
            set { txtCurrent.Text = value; }
        }

        public string NewVal
        {
            get { return txtNew.Text; }
        }

        public bool IsString
        {
            get { return rbString.Checked; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
        private void BtnOkClick(object sender, EventArgs e)
        {
            if (rbInteger.Checked && !Valid())
            {
                MessageBox.Show(@"The new value is not an Integer", @"Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                DialogResult = DialogResult.Cancel;
            }
            else
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void BtnCancelClick(object sender, EventArgs e)
        {
            Close();
        }

        private void RbIntegerCheckedChanged(object sender, EventArgs e)
        {
            Valid();
        }

        private bool Valid()
        {
            int test; 
            
            if (rbInteger.Checked && !int.TryParse(txtNew.Text, out test))
            {
                txtNew.Text = string.Empty;

                return false;
            }

            return true;
        }
    }
}
