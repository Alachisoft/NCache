using System;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;

namespace Lextm.Common
{
    public class SingleInstanceController : WindowsFormsApplicationBase
    {
        private readonly Type _mainForm;
        
        public SingleInstanceController(Type mainForm)
        {
            if (mainForm == null)
            {
                throw new ArgumentNullException("mainForm");
            }
            
            IsSingleInstance = true; 
            _mainForm = mainForm;            
        } 
        
        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            MainForm.Show();
            MainForm.WindowState = FormWindowState.Normal;
            base.OnStartupNextInstance(eventArgs);
        }
        
        protected override void OnCreateMainForm()
        {            
            var handler = MainFormCreated;
            if (handler != null)
            {
                handler(null, EventArgs.Empty);
            }
            
            MainForm = (Form)Activator.CreateInstance(_mainForm);
        }
        
        public event EventHandler<EventArgs> MainFormCreated;
    }
}