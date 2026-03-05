/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/7/20
 * Time: 20:32
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Lextm.Common;
using Lextm.SharpSnmpLib.Mib;
using RemObjects.Mono.Helpers;
using WeifenLuo.WinFormsUI.Docking;

namespace Lextm.SharpSnmpLib.Browser
{
    /// <summary>
    /// Description of ModuleListPanel.
    /// </summary>
// ReSharper disable UnusedMember.Global
    internal partial class ModuleListPanel : DockContent
// ReSharper restore UnusedMember.Global
    {
        private const string Filter = "*.module";
        private FileSystemWatcher _watcher;
        private Watchdog _dog;

        public ModuleListPanel()
        {
            InitializeComponent();
            if (PlatformSupport.Platform != PlatformType.Windows)
            {
                return;
            }

            Icon = Properties.Resources.preferences_system_windows;
            actEnableMonitor.Image = Properties.Resources.view_refresh;
            actRemove.Image = Properties.Resources.list_remove;
            actAdd.Image = Properties.Resources.list_add;
        }
        
        private void ModuleListPanelLoad(object sender, EventArgs e)
        {
            Objects.OnChanged += RefreshPanel;
            RefreshPanel(Objects, EventArgs.Empty);

            _dog = new Watchdog(10000d);
            _dog.Bark += DogBark;
            _dog.Enabled = actEnableMonitor.Checked;

            _watcher = new FileSystemWatcher(((ReloadableObjectRegistry)Objects).Path, Filter)
            {
                IncludeSubdirectories = false
            };
            _watcher.Changed += OnChanged;
            _watcher.Created += OnChanged;
            _watcher.Deleted += OnChanged;
            _watcher.EnableRaisingEvents = actEnableMonitor.Checked;
        }

        private void DogBark(object sender, EventArgs e)
        {
            ((ReloadableObjectRegistry)Objects).Reload();
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            _dog.Feed();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public IObjectRegistry Objects { get; set; }

        private void RefreshPanel(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)(() => RefreshPanel(sender, e)));
                return;
            }

            ReloadableObjectRegistry reg = (ReloadableObjectRegistry)sender;
            SuspendLayout();
            listView1.Items.Clear();
            List<string> loaded = new List<string>(reg.Tree.LoadedModules);
            loaded.Sort();
            foreach (ListViewItem item in loaded.Select(module => listView1.Items.Add(module)))
            {
                item.Group = listView1.Groups["lvgLoaded"];
            }
            
            string[] files = Directory.GetFiles(reg.Path, Filter);
            foreach (ListViewItem item in from file in files
                     select Path.GetFileNameWithoutExtension(file)
                     into name 
                     where !loaded.Contains(name) 
                     select listView1.Items.Add(name))
            {
                item.BackColor = Color.LightGray;
                item.Group = listView1.Groups["lvgPending"];
            }
            
            ResumeLayout();
            listView1.Groups["lvgLoaded"].Header = string.Format(CultureInfo.CurrentCulture, "Loaded ({0})", listView1.Groups["lvgLoaded"].Items.Count);
            listView1.Groups["lvgPending"].Header = string.Format(CultureInfo.CurrentCulture, "Unloaded ({0})", listView1.Groups["lvgPending"].Items.Count);
            tslblCount.Text = string.Format(CultureInfo.InvariantCulture, "loaded: {0}; unloaded: {1}", listView1.Groups["lvgLoaded"].Items.Count, listView1.Groups["lvgPending"].Items.Count);
        }

        private void ActAddExecute(object sender, EventArgs e)
        {
            ReloadableObjectRegistry reg = (ReloadableObjectRegistry)Objects;
            string index = Path.Combine(reg.Path, "index");
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                string name = item.Text;
                
                // TODO: bad performance. improve later.
                List<string> list = new List<string>(File.ReadAllLines(index));
                if (list.Contains(name))
                {
                    continue;
                }

                list.Add(name);
                File.WriteAllLines(index, list.ToArray());
            }

            reg.Reload();
        }

        private void ActRemoveExecute(object sender, EventArgs e)
        {
            ReloadableObjectRegistry reg = (ReloadableObjectRegistry)Objects;
            string index = Path.Combine(reg.Path, "index");
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                string name = item.Text;
                
                // TODO: bad performance. improve later.
                List<string> list = new List<string>(File.ReadAllLines(index));
                if (!list.Contains(name))
                {
                    continue;
                }

                list.Remove(name);
                File.WriteAllLines(index, list.ToArray());
            }

            reg.Reload();
        }

        private void ActRemoveUpdate(object sender, EventArgs e)
        {
            actRemove.Enabled = listView1.SelectedItems.Count > 0 && ItemsInGroup(listView1.SelectedItems, "lvgLoaded");
        }

        private static bool ItemsInGroup(IEnumerable collection, string group)
        {
            return collection.Cast<ListViewItem>().All(item => item.Group.Name == group);
        }

        private void ActAddUpdate(object sender, EventArgs e)
        {
            actAdd.Enabled = listView1.SelectedItems.Count > 0 && ItemsInGroup(listView1.SelectedItems, "lvgPending");
        }

        private void ListView1MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                contextModuleMenu.Show(listView1, e.Location);
            }
        }

        private void ActEnableMonitorExecute(object sender, EventArgs e)
        {
            _dog.Enabled = actEnableMonitor.Checked;
            _watcher.EnableRaisingEvents = actEnableMonitor.Checked;
        }
    }
}
