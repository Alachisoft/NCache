using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using Lextm.SharpSnmpLib.Messaging;
using Microsoft.Practices.Unity;
using RemObjects.Mono.Helpers;

namespace Lextm.SharpSnmpLib.Browser
{
    internal partial class FormTable : Form
    {
        private readonly IDefinition _definition;
        private bool _columnCountSet;
        private Thread _refreshThread;

        internal delegate void RefreshTableCallback(IList<Variable> list);

        public FormTable(IDefinition def)
        {
            _definition = def;
            InitializeComponent();
            cbColumnDisplay.SelectedIndex = 1;
            if (PlatformSupport.Platform == PlatformType.Windows)
            {
                Icon = Properties.Resources.x_office_spreadsheet;
            }
        }

        public void SetRows(int rowCount)
        {
            dataGridTable.RowCount = rowCount;
        }

        public void PopulateGrid(IList<Variable> list)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (dataGridTable.InvokeRequired)
            {
                RefreshTableCallback r = PopulateGrid;
                Invoke(r, new object[] { list });
            }
            else 
            {
                int rowCount = dataGridTable.RowCount;

                dataGridTable.ColumnCount = list.Count / dataGridTable.RowCount;
                dataGridTable.RowCount = 0;
                _columnCountSet = true;

                CreateColumns();

                for (int x = 0; x < rowCount; x++)
                {
                    string[] row = new string[dataGridTable.ColumnCount];

                    for (int y = 0; y < dataGridTable.ColumnCount; y++)
                    {
                        row[y] = list[(y * rowCount) + x].Data.ToString();
                    }

                    dataGridTable.Rows.Add(row);
                    Refresh();
                }
            }
        }

        private void CheckBoxRefreshCheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxRefresh.Checked)
            {
                _refreshThread = new Thread(RefreshTable);
                _refreshThread.Start();
            }
            else
            {
                _refreshThread.Abort();
            }
        }

        private void CreateColumns()
        {
            int x = 0;
            
            // We want to move into the table object here
            IEnumerator<IDefinition> i = _definition.Children.GetEnumerator();
            i.Reset();
            i.MoveNext();
            IDefinition d = i.Current;

            if (d == null)
            {
                return;
            }

            foreach (IDefinition def in d.Children)
            {
                dataGridTable.Columns[x++].Name = cbColumnDisplay.SelectedIndex == 0 ? new SearchResult(def).AlternativeText : def.Name;
            }
        }

        private void CbColumnDisplaySelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_columnCountSet)
            {
                return;
            }

            CreateColumns();
            Refresh();
        }

        private void RefreshTable()
        {
            // TODO: how to get rid of infinite loop?
            // will use WatchDog class to optimize this.
            while (true)
            {
                IProfileRegistry registry = Program.Container.Resolve<IProfileRegistry>();
                if (registry == null)
                {
                    break;
                }

                IList<Variable> list = new List<Variable>();
                Thread.Sleep(Convert.ToInt32(textBoxRefresh.Text, CultureInfo.CurrentCulture) * 1000);
                NormalAgentProfile prof = registry.DefaultProfile as NormalAgentProfile;
                int rows;
                if (prof != null)
                {
                    rows = Messenger.Walk(
                        prof.VersionCode, 
                        prof.Agent, 
                        prof.GetCommunity,
                        new ObjectIdentifier(_definition.GetNumericalForm()),
                        list, 
                        prof.Timeout,
                        WalkMode.WithinSubtree);                    
                }
                else
                {
                    SecureAgentProfile p = (SecureAgentProfile)registry.DefaultProfile;
                    Discovery discovery = Messenger.NextDiscovery;
                    ReportMessage report = discovery.GetResponse(p.Timeout, p.Agent);
                    rows = Messenger.BulkWalk(
                        VersionCode.V3, 
                        p.Agent, 
                        new OctetString(p.UserName),
                        new ObjectIdentifier(_definition.GetNumericalForm()), 
                        list, 
                        p.Timeout,
                        10,
                        WalkMode.WithinSubtree, 
                        p.Privacy, 
                        report);
                }

                SetRows(rows);
                PopulateGrid(list);
            }
        }

        private void TextBoxRefreshKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.KeyValue > 0 && e.KeyValue < 31) || (e.KeyValue > 47 && e.KeyValue < 58) || (e.KeyValue > 95 && e.KeyValue < 106))
            {
                // Allow all numbers to be printed
                return;
            }
            
            e.SuppressKeyPress = true;
        }

        private void FormTableFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_refreshThread != null)
            {
                _refreshThread.Abort();
            }
        }
    }
}
