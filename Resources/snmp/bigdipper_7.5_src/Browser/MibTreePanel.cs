/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/6/28
 * Time: 15:25
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

using Lextm.SharpSnmpLib.Mib;
using RemObjects.Mono.Helpers;
using WeifenLuo.WinFormsUI.Docking;

namespace Lextm.SharpSnmpLib.Browser
{
    /// <summary>
    /// Description of MibTreePanel.
    /// </summary>
    internal partial class MibTreePanel : DockContent
    {
        private bool _showNumber;
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger("Lextm.SharpSnmpLib.Browser");

        public MibTreePanel()
        {
            InitializeComponent();
            if (PlatformSupport.Platform == PlatformType.Windows)
            {
                actNumber.Image = Properties.Resources.office_calendar;
            }
        }

        public IObjectRegistry Objects { get; set; }

        public IProfileRegistry Profiles { get; set; }

        private void RefreshPanel(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)(() => RefreshPanel(sender, e)));
                return;
            }

            ReloadableObjectRegistry repository = (ReloadableObjectRegistry)sender;
            treeView1.Nodes.Clear();
            TreeNode root = Wrap(repository.Tree.Root);
            
            // FIXME: worked around a Mono issue.
            treeView1.Nodes.AddRange(root.Nodes.Cast<TreeNode>().ToArray());
        }

        private TreeNode Wrap(IDefinition definition)
        {
            string name = _showNumber ? string.Format(CultureInfo.InvariantCulture, "{0}({1})", definition.Name, definition.Value) : definition.Name;
            TreeNode node = new TreeNode(name)
                                {
                                    Tag = definition,
                                    ImageIndex = (int)definition.Type,
                                    SelectedImageIndex = (int)definition.Type,
                                    ToolTipText =
                                        new SearchResult(definition).AlternativeText + Environment.NewLine +
                                        definition.Value
                                };

            List<IDefinition> list = new List<IDefinition>(definition.Children);
            list.Sort(new DefinitionComparer());
            foreach (IDefinition def in list)
            {
                node.Nodes.Add(Wrap(def));
            }

            return node;
        }

        private static ObjectIdentifier GetIdForGet(IDefinition def)
        {
            if (def.Type == DefinitionType.Scalar)
            {
                return ObjectIdentifier.Create(def.GetNumericalForm(), 0);
            }

            uint index;
            using (FormIndex form = new FormIndex())
            {
                form.ShowDialog();
                index = form.Index;
            }

            return ObjectIdentifier.Create(def.GetNumericalForm(), index);
        }

        private static ObjectIdentifier GetIdForGetNext(IDefinition def)
        {
            return def.Type == DefinitionType.Scalar ? ObjectIdentifier.Create(def.GetNumericalForm(), 0) : new ObjectIdentifier(def.GetNumericalForm());
        }

        private void ActGetExecute(object sender, EventArgs e)
        {
            try
            {
                Logger.Info("==== Begin GET ====");
                ObjectIdentifier id = GetIdForGet((IDefinition)treeView1.SelectedNode.Tag);
                Profiles.DefaultProfile.Get(new Variable(id));
            }
            catch (Exception ex)
            {
                Logger.Info(ex.ToString());
            }
            finally
            {
                Logger.Info("==== End GET ====");
            }
        }

        private void ActGetUpdate(object sender, EventArgs e)
        {
            actGet.Enabled = ValidForGet(treeView1.SelectedNode);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
        private void ActSetExecute(object sender, EventArgs e)
        {
            try
            {
                ISnmpData data;
                using (FormSet form = new FormSet())
                {
                    ObjectIdentifier id = GetIdForGet((IDefinition)treeView1.SelectedNode.Tag);
                    form.OldVal = Profiles.DefaultProfile.GetValue(new Variable(id));
                    if (form.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                    
                    if (form.IsString)
                    {
                        data = new OctetString(form.NewVal);
                    }
                    else
                    {
                        int result;
                        if (!int.TryParse(form.NewVal, out result))
                        {
                            MessageBox.Show(
                                @"Value entered was not an Integer!", 
                                @"SNMP Set Error",
                                MessageBoxButtons.OK, 
                                MessageBoxIcon.Error);
                            return;
                        }

                        data = new Integer32(result);
                    }
                }

                Logger.Info("==== Begin SET ====");
                ObjectIdentifier id1 = GetIdForGet((IDefinition)treeView1.SelectedNode.Tag);
                Profiles.DefaultProfile.Set(new Variable(id1, data));
            }
            catch (Exception ex)
            {
                Logger.Info(ex.ToString());
            }
            finally
            {
                Logger.Info("==== End SET ====");
            }
        }

        private void ActSetUpdate(object sender, EventArgs e)
        {
            actSet.Enabled = ValidForGet(treeView1.SelectedNode);
        }

        private static bool ValidForGet(TreeNode node)
        {
            if (node == null)
            {
                return false;
            }

            // Scalar or Column. (see DefinitionType.cs)
            return node.ImageIndex == 2 || node.ImageIndex == 5;
        }

        private static bool ValidForGetNext(TreeNode node)
        {
            return node != null && node.Level > 0;
        }

        private void ActGetTableExecute(object sender, EventArgs e)
        {
            try
            {
                Profiles.DefaultProfile.GetTable((IDefinition)treeView1.SelectedNode.Tag);
            }
            catch (Exception ex)
            {
                Logger.Info(ex.ToString());
            }
        }

        private void ActGetTableUpdate(object sender, EventArgs e)
        {
            actGetTable.Enabled = treeView1.SelectedNode != null && treeView1.SelectedNode.ImageIndex == 3;
        }

        private void TreeView1AfterSelect(object sender, TreeViewEventArgs e)
        {
            tslblOID.Text = ObjectIdentifier.Convert(((IDefinition)e.Node.Tag).GetNumericalForm());
            if (ValidForGet(e.Node))
            {
                ActGetExecute(sender, e);
            }
            else if (ValidForGetTable(e.Node))
            {
                ActGetTableExecute(sender, e);
            }
        }

        private static bool ValidForGetTable(TreeNode node)
        {
            return node != null && node.ImageIndex == 3;
        }

        private void ActGetNextExecute(object sender, EventArgs e)
        {
            try
            {
                Logger.Info("==== Begin GET NEXT ====");
                ObjectIdentifier id = GetIdForGetNext((IDefinition)treeView1.SelectedNode.Tag);
                Profiles.DefaultProfile.GetNext(new Variable(id));
            }
            catch (Exception ex)
            {
                Logger.Info(ex.ToString());
            }
            finally
            {
                Logger.Info("==== End GET NEXT ====");
            }
        }

        private void ActGetNextUpdate(object sender, EventArgs e)
        {
            actGetNext.Enabled = ValidForGetNext(treeView1.SelectedNode);
        }

        private void MibTreePanelLoad(object sender, EventArgs e)
        {
            RefreshPanel(Objects, EventArgs.Empty);
            Objects.OnChanged += RefreshPanel;
        }

        private void ActNumberExecute(object sender, EventArgs e)
        {
            _showNumber = !_showNumber;
            RefreshPanel(Objects, EventArgs.Empty);
        }

        private void ActWalkExecute(object sender, EventArgs e)
        {
            try
            {
                Logger.Info("==== Begin WALK ====");
                Profiles.DefaultProfile.Walk((IDefinition)treeView1.SelectedNode.Tag);                
            }
            catch (Exception ex)
            {
                Logger.Info(ex.ToString());
            }
            finally
            {
                Logger.Info("==== End WALK ====");
            }
        }

        private void ActWalkUpdate(object sender, EventArgs e)
        {
            actWalk.Enabled = ValidForGetNext(treeView1.SelectedNode);
        }

        #region Nested type: DefinitionComparer

        private class DefinitionComparer : IComparer<IDefinition>
        {
            #region IComparer<IDefinition> Members

            public int Compare(IDefinition x, IDefinition y)
            {
                return x.Value.CompareTo(y.Value);
            }

            #endregion
        }

        #endregion

        private void ActGetNumericExecute(object sender, EventArgs e)
        {
            Clipboard.SetText(ObjectIdentifier.Convert(((IDefinition)treeView1.SelectedNode.Tag).GetNumericalForm()));
        }

        private void ActGetNumericUpdate(object sender, EventArgs e)
        {
            actGetNumeric.Enabled = treeView1.SelectedNode != null;
            actGetTextual.Enabled = treeView1.SelectedNode != null;
        }

        private void ActGetTextualExecute(object sender, EventArgs e)
        {
            Clipboard.SetText(new SearchResult((IDefinition)treeView1.SelectedNode.Tag).AlternativeText);
        }
    }
}
