/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/6/28
 * Time: 15:33
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Globalization;
using System.Windows.Forms;
using RemObjects.Mono.Helpers;
using WeifenLuo.WinFormsUI.Docking;

namespace Lextm.SharpSnmpLib.Compiler
{
    /// <summary>
    /// Description of OutputPanel.
    /// </summary>
    internal partial class OutputPanel : DockContent, IOutputPanel
    {
        public OutputPanel()
        {
            InitializeComponent();
            if (PlatformSupport.Platform == PlatformType.Windows)
            {
                Icon = Properties.Resources.utilities_terminal;
            }
        }

        public void Write(string message)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)(() => Write(message)));
                return;
            }

            txtMessages.AppendText(string.Format(CultureInfo.InvariantCulture, "[{0}] {1}", DateTime.Now, message));
            txtMessages.ScrollToCaret();
        }

        private void ActClearExecute(object sender, EventArgs e)
        {
            txtMessages.Clear();
        }

        private void TxtMessagesMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                contextOuputMenu.Show(txtMessages, e.Location);
            }
        }
    }
}
