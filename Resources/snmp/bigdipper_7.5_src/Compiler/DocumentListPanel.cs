/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2009/1/28
 * Time: 18:00
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using RemObjects.Mono.Helpers;
using WeifenLuo.WinFormsUI.Docking;

namespace Lextm.SharpSnmpLib.Compiler
{
    /// <summary>
    /// Description of DocumentListPanel.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal partial class DocumentListPanel : DockContent
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger("Lextm.SharpSnmpLib.Compiler");

        public DocumentListPanel()
        {
            InitializeComponent();
            if (PlatformSupport.Platform == PlatformType.Windows)
            {                
                Icon = Properties.Resources.document_new;
            }
        }
        
        private void LvFilesDoubleClick(object sender, EventArgs e)
        {
            string fileName = lvFiles.SelectedItems[0].Tag.ToString();
            OpenDocument(fileName);
        }

        private void OpenDocument(string fileName)
        {
            IDockContent opened = FindDocument(fileName);
            if (opened != null)
            {
                opened.DockHandler.Show();
                Logger.InfoFormat(CultureInfo.InvariantCulture, "The document: {0} has already opened!", fileName);
                return;
            }
            
            DocumentPanel doc = new DocumentPanel(fileName);
            doc.Show(DockPanel, DockState.Document);
        }
        
        private IDockContent FindDocument(string fileName)
        {
            if (DockPanel.DocumentStyle == DocumentStyle.SystemMdi)
            {
                throw new InvalidOperationException("cannot be System MDI");
            }

            return DockPanel.Documents.FirstOrDefault(content => content.DockHandler.TabText == fileName);
        }

        private void DocumentListPanelLoad(object sender, EventArgs e)
        {
            Compiler.FileAdded += FileOpen;
        }

        private void FileOpen(object sender, FileAddedEventArgs e)
        {
            foreach (string file in e.Files)
            {
                ListViewItem item = lvFiles.Items.Add(Path.GetFileName(file));
                item.Tag = file;
            }

            if (e.Files.Count > 0)
            {
                OpenDocument(e.Files[0]);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public CompilerCore Compiler { get; set; }

        private void ActDeleteUpdate(object sender, EventArgs e)
        {
            actDelete.Enabled = lvFiles.Items.Count > 0;
        }

        private void ActDeleteExecute(object sender, EventArgs e)
        {
            string fileName = lvFiles.SelectedItems[0].Tag.ToString();
            lvFiles.SelectedItems[0].Remove();
            Compiler.Remove(fileName);
        }
    }
}
