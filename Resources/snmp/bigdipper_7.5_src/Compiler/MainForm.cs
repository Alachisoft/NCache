/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/6/28
 * Time: 12:15
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Practices.Unity;
using RemObjects.Mono.Helpers;
using WeifenLuo.WinFormsUI.Docking;

namespace Lextm.SharpSnmpLib.Compiler
{
    /// <summary>
    /// Description of MainForm.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            if (PlatformSupport.Platform == PlatformType.Windows)
            {
                Icon = Properties.Resources.accessories_text_editor;
                actAbout.Image = Properties.Resources.help_browser;
                actCompileAll.Image = Properties.Resources.go_jump;
                actCompile.Image = Properties.Resources.go_bottom;
                actOpen.Image = Properties.Resources.document_open;
                actExit.Image = Properties.Resources.system_log_out;
            }

            DockContent files = Program.Container.Resolve<DockContent>("DocumentList");
            files.Show(dockPanel1, DockState.DockLeft);

            DockContent output = Program.Container.Resolve<DockContent>("Output");
            output.Show(dockPanel1, DockState.DockBottom);

            DockContent modules = Program.Container.Resolve<DockContent>("ModuleList");
            modules.Show(dockPanel1, DockState.DockRight);
            
            Compiler = Program.Container.Resolve<CompilerCore>();
        }

        public CompilerCore Compiler { get; private set; }

        private void ActExitExecute(object sender, EventArgs e)
        {
            Close();
        }

        private void ActOpenExecute(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            dockPanel1.SuspendLayout(true);
            Compiler.Add(openFileDialog1.FileNames);

            dockPanel1.ResumeLayout(true, true);
        }

        private void ActCompileExecute(object sender, EventArgs e)
        {
            IDockContent content = dockPanel1.ActiveDocument;
            DocumentPanel doc = (DocumentPanel)content;
            Compiler.Compile(new [] { doc.FileName });
        }

        private void ActCompileUpdate(object sender, EventArgs e)
        {
            actCompile.Enabled = dockPanel1.ActiveDocument != null && !Compiler.IsBusy;
        }

        private void ActCompileAllExecute(object sender, EventArgs e)
        {
            Compiler.CompileAll();
        }

        private void ActCompileAllUpdate(object sender, EventArgs e)
        {
            actCompileAll.Enabled = dockPanel1.DocumentsCount > 0 && !Compiler.IsBusy;
        }
        
        private void ActAboutExecute(object sender, EventArgs e)
        {
            Process.Start("http://sharpsnmplib.codeplex.com");
        }

        private void MainFormLoad(object sender, EventArgs e)
        {
            Text = string.Format(CultureInfo.CurrentUICulture, "{0} (Version: {1})", Text, Assembly.GetExecutingAssembly().GetName().Version);
        }
        
        private void MainFormClosing(object sender, FormClosingEventArgs e)
        {
            // FIXME: work around a DPS disposing infinite loop.
            foreach (var d in Program.Container.ResolveAll<DockContent>())
            {
                d.Close();
                d.Dispose();
            }
        }
    }
}
