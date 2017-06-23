//===============================================================================
// Copyright© 2010 Alachisoft.  All rights reserved.
//===============================================================================

using System;
using System.Collections;
using System.ComponentModel;
using System.Web;
using System.Web.SessionState;
using System.Configuration;

namespace guessgame 
{
	/// <summary>
	/// Summary description for Global.
	/// </summary>
    public class Global : HttpApplication
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public Global()
        {
            InitializeComponent();
        }

        public void Session_End(object o, EventArgs e)
        {
            Session.Abandon();
        }

        #region Web Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
        }
        #endregion
    }
}

