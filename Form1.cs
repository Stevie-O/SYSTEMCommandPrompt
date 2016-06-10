using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SYSTEMCommandPrompt
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnOpenCommandPromptSYSTEM_Click(object sender, EventArgs e)
        {
            txtOutput.Clear();
            TextWriter tw = new TextBoxWriter(txtOutput);
            btnOpenCommandPromptSYSTEM.Enabled = false;

            SystemCommandPromptLauncher launcher = new SystemCommandPromptLauncher(tw);

            Thread t = new Thread(DoLaunch);
            t.Name = "SystemCommandPromptLauncher";
            t.Start(launcher);
        }

        bool closeRequested;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!btnOpenCommandPromptSYSTEM.Enabled)
            {
                e.Cancel = true;
                txtOutput.AppendText("Application will close shortly...\r\n");
                closeRequested = true;
            }

            base.OnFormClosing(e);
        }

        void DoLaunch(object arg)
        {
            SystemCommandPromptLauncher launcher = (SystemCommandPromptLauncher)arg;
            Exception failure = null;
            try
            {
                launcher.LaunchIt();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            BeginInvoke((MethodInvoker)delegate ()
            {
                AfterLaunch(failure);
            });
        }

        void AfterLaunch(Exception ex)
        {
            if (ex != null)
                MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnOpenCommandPromptSYSTEM.Enabled = true;
            txtOutput.AppendText("Done!\r\n");
            if (closeRequested)
                this.Close();
        }

    }
}
