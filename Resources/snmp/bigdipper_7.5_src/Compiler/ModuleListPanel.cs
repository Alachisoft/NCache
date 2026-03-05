/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/7/20
 * Time: 20:32
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using Lextm.SharpSnmpLib.Mib;
using RemObjects.Mono.Helpers;
using WeifenLuo.WinFormsUI.Docking;

namespace Lextm.SharpSnmpLib.Compiler
{
    /// <summary>
    /// Description of ModuleListPanel.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal partial class ModuleListPanel : DockContent
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger("Lextm.SharpSnmpLib.Compiler");

        public ModuleListPanel()
        {
            InitializeComponent();
            if (PlatformSupport.Platform == PlatformType.Windows)
            {
                Icon = Properties.Resources.preferences_system_windows;
            }
        }

        private void ModuleListPanelLoad(object sender, EventArgs e)
        {
            // TODO: bind this refresh to assembler event.
            RefreshPanel(this, EventArgs.Empty);
            if (Compiler != null)
            {
                Compiler.RunCompilerCompleted += RefreshPanel;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public CompilerCore Compiler { get; set; }

        private void RefreshPanel(object sender, EventArgs e)
        {
            SuspendLayout();
            listView1.Items.Clear();
            List<string> loaded = new List<string>(Assembler.Tree.LoadedModules);
            loaded.Sort();
            foreach (ListViewItem item in loaded.Select(module => listView1.Items.Add(module)))
            {
                item.Group = listView1.Groups["lvgLoaded"];
            }
            
            List<string> pendings = new List<string>(Assembler.Tree.PendingModules);
            pendings.Sort();
            foreach (ListViewItem item in pendings.Select(pending => listView1.Items.Add(pending)))
            {
                item.BackColor = Color.LightGray;
                item.Group = listView1.Groups["lvgPending"];
            }
            
            ResumeLayout();
            listView1.Groups["lvgLoaded"].Header = string.Format(CultureInfo.InvariantCulture, "Loaded ({0})", Assembler.Tree.LoadedModules.Count);
            listView1.Groups["lvgPending"].Header = string.Format(CultureInfo.InvariantCulture, "Pending ({0})", Assembler.Tree.PendingModules.Count);
            tslblCount.Text = string.Format(CultureInfo.InvariantCulture, "loaded: {0}; unloaded: {1}", Assembler.Tree.LoadedModules.Count, Assembler.Tree.PendingModules.Count);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public Assembler Assembler { get; set; }

        private void ActRemoveExecute(object sender, EventArgs e)
        {
            string mib = listView1.SelectedItems[0].Text;
            File.Delete(Path.Combine(Assembler.Folder, mib + ".module"));
            Logger.Info("The change will take effect when you restart compiler");
            Assembler.Tree.Remove(mib);
        }

        private void ActRemoveUpdate(object sender, EventArgs e)
        {
            actRemove.Enabled = listView1.SelectedItems.Count > 0;
        }

        private void ListView1MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                contextModuleMenu.Show(listView1, e.Location);
            }
        }
    }
}
