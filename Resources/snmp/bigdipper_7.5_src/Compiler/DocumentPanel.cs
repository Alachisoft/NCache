using System;
using System.IO;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using RemObjects.Mono.Helpers;
using WeifenLuo.WinFormsUI.Docking;

namespace Lextm.SharpSnmpLib.Compiler
{
    internal partial class DocumentPanel : DockContent
    {
        private readonly string _fileName;

        public DocumentPanel(string fileName)
        {
            InitializeComponent();
            _fileName = fileName;
            TabText = _fileName;
            if (PlatformSupport.Platform == PlatformType.Windows)
            {
                Icon = Properties.Resources.document_properties;

                // SharpDevelop text editor is Windows only.
                var txtDocument = new TextEditorControl {Dock = DockStyle.Fill, IsReadOnly = true, Name = "txtDocument"};
                Controls.Add(txtDocument);

                txtDocument.SetHighlighting("SMI"); // Activate the highlighting, use the name from the SyntaxDefinition node.
                txtDocument.LoadFile(_fileName);
            }
            else
            {
                var txtDocument = new RichTextBox {Dock = DockStyle.Fill, Name = "txtDocument", ReadOnly = true};
                Controls.Add(txtDocument);

                txtDocument.LoadFile(_fileName, RichTextBoxStreamType.PlainText);
            }
        }

        public string FileName
        {
            get { return _fileName; }
        }

        private void CloseAllToolStripMenuItemClick(object sender, EventArgs e)
        {
            CloseAllDocuments(DockPanel);
        }
        
        private static void CloseAllDocuments(DockPanel panel)
        {
            if (panel.DocumentStyle == DocumentStyle.SystemMdi)
            {
                throw new InvalidOperationException("cannot work in System MDI mode");
            }
            
            IDockContent[] documents = panel.DocumentsToArray();
            foreach (IDockContent content in documents)
            {
                content.DockHandler.Close();
            }
        }
    }
}
