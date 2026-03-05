using System;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Windows.Forms;
using RemObjects.Mono.Helpers;

namespace Lextm.SharpSnmpLib.Browser
{
    internal partial class FormProfile : Form
    {
        private readonly AgentProfile _profile;

        public FormProfile(AgentProfile agent)
        {
            InitializeComponent();
            _profile = agent;
            if (PlatformSupport.Platform == PlatformType.Windows)
            {
                Icon = Properties.Resources.network_server;
            }
        }

        internal IPAddress IP
        {
            get { return IPAddress.Parse(txtIP.Text); }
        }

        internal VersionCode VersionCode
        {
            get { return VersionCodeExtension.FromSelectedIndex(cbVersionCode.SelectedIndex); }
        }

        internal string AgentName
        {
            get { return txtName.Text; }
        }

        internal string GetCommunity
        {
            get { return txtGet.Text; }
        }

        internal string SetCommunity
        {
            get { return txtSet.Text; }
        }
        
        internal int Port
        {
            get { return int.Parse(txtPort.Text, CultureInfo.CurrentCulture); }
        }

        public string AuthenticationPassphrase
        {
            get { return txtAuthentication.Text; }
        }

        public string PrivacyPassphrase
        {
            get { return txtPrivacy.Text; }
        }

        public int AuthenticationMethod
        {
            get
            {
                return cbAuthentication.SelectedIndex;
            }
        }

        public int PrivacyMethod
        {
            get { return cbPrivacy.SelectedIndex; }
        }

        public string UserName
        {
            get { return txtUserName.Text; }
        }

        private void FormProfileLoad(object sender, EventArgs e)
        {
            if (_profile == null)
            {
                cbVersionCode.SelectedIndex = 1;
                cbAuthentication.SelectedIndex = 0;
                cbPrivacy.SelectedIndex = 0;
                return;
            }
            
            txtIP.Text = _profile.Agent.Address.ToString();
            txtIP.ReadOnly = true;
            txtName.Text = _profile.Name;
            cbVersionCode.SelectedIndex = _profile.VersionCode.ToSelectedIndex();

            var normal = _profile as NormalAgentProfile;
            txtGet.Text = normal == null ? string.Empty : normal.GetCommunity.ToString();
            txtSet.Text = normal == null ? string.Empty : normal.SetCommunity.ToString();

            var secure = _profile as SecureAgentProfile;
            txtAuthentication.Text = secure == null ? string.Empty : secure.AuthenticationPassphrase;
            txtPrivacy.Text = secure == null ? string.Empty : secure.PrivacyPassphrase;
            cbAuthentication.SelectedIndex = secure == null ? 0 : secure.AuthenticationMethod;
            cbPrivacy.SelectedIndex = secure == null ? 0 : secure.PrivacyMethod;
            txtUserName.Text = _profile.UserName;
        }
        
        private void TxtPortValidating(object sender, CancelEventArgs e)
        {
            int result;
            bool succeeded = int.TryParse(txtPort.Text, out result);
            if (succeeded && result > 0)
            {
                return;
            }

            e.Cancel = true;
            txtPort.SelectAll();
            errorProvider1.SetError(txtPort, "Please provide a valid port number");
        }
        
        private void TxtPortValidated(object sender, EventArgs e)
        {
            errorProvider1.SetError(txtPort, string.Empty);
        }
        
        private void TxtSetValidating(object sender, CancelEventArgs e)
        {
            ValidatingTextBox(txtSet, e);
        }
        
        private void TxtSetValidated(object sender, EventArgs e)
        {
            errorProvider1.SetError(txtSet, string.Empty);
        }

        private void TxtGetValidating(object sender, CancelEventArgs e)
        {
            ValidatingTextBox(txtGet, e);
        }
        
        private void TxtGetValidated(object sender, EventArgs e)
        {
            errorProvider1.SetError(txtGet, string.Empty);
        }

        private void TxtIpValidating(object sender, CancelEventArgs e)
        {
            IPAddress ip;
            if (AgentProfile.IsValid(txtIP.Text, out ip))
            {
                return;
            }

            e.Cancel = true;
            txtIP.SelectAll();
            errorProvider1.SetError(txtIP, "IP address is not valid");
        }
        
        private void TxtIpValidated(object sender, EventArgs e)
        {
            errorProvider1.SetError(txtIP, string.Empty);
        }
        
        private void BtnOkClick(object sender, EventArgs e)
        {
            ValidateAllChildren(this);
        }
        
        private void ValidateAllChildren(Control parent)
        {
            if (DialogResult == DialogResult.None)
            {
                return;    
            }
            
            foreach (Control c in parent.Controls)
            {
                c.Focus();
                if (!Validate())
                {
                    DialogResult = DialogResult.None;
                    return;
                }
                
                ValidateAllChildren(c);
            }
        }

        private void CbVersionCodeSelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateControls();
        }

        private void UpdateControls()
        {
            bool version3Detected = cbVersionCode.SelectedIndex == 2;
            bool authenEnabled = cbAuthentication.SelectedIndex != 0;
            bool privEnabled = cbPrivacy.SelectedIndex != 0;

            cbAuthentication.Enabled = version3Detected;
            cbPrivacy.Enabled = version3Detected && authenEnabled;
            txtAuthentication.Enabled = version3Detected && authenEnabled;
            txtPrivacy.Enabled = version3Detected && privEnabled && authenEnabled;
            txtUserName.Enabled = version3Detected;
            txtGet.Enabled = !version3Detected;
            txtSet.Enabled = !version3Detected;
        }

        private void TxtAuthenticationValidated(object sender, EventArgs e)
        {
            errorProvider1.SetError(txtAuthentication, string.Empty);
        }

        private void TxtAuthenticationValidating(object sender, CancelEventArgs e)
        {
            ValidatingTextBox(txtAuthentication, e);
        }

        private void TxtPrivacyValidated(object sender, EventArgs e)
        {
            errorProvider1.SetError(txtPrivacy, string.Empty);
        }

        private void TxtPrivacyValidating(object sender, CancelEventArgs e)
        {
            ValidatingTextBox(txtPrivacy, e);
        }

        private void TxtUserNameValidated(object sender, EventArgs e)
        {
            errorProvider1.SetError(txtUserName, string.Empty);
        }

        private void TxtUserNameValidating(object sender, CancelEventArgs e)
        {
            ValidatingTextBox(txtUserName, e);
        }

        private void ValidatingTextBox(TextBoxBase control, CancelEventArgs e)
        {
            if (!control.Enabled)
            {
                return;
            }

            if (control.Text.Length != 0)
            {
                return;
            }

            e.Cancel = true;
            control.SelectAll();
            errorProvider1.SetError(control, "Text box cannot be empty");
        }

        private void CbAuthenticationSelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateControls();
        }

        private void CbPrivacySelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateControls();
        }
    }
}
