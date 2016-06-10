using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace SYSTEMCommandPrompt
{
    [System.ComponentModel.DesignerCategory("")]
    partial class ProcessLauncherSvc : ServiceBase
    {
        public ProcessLauncherSvc()
        {
            InitializeComponent();
        }

        public string EventName;
        public int Pid;

        protected override void OnStart(string[] args)
        {
            // okay, the 'args' here are ONLY for when the service is started via the Properties dialog in services.msc
            EventWaitHandle ewh = EventWaitHandle.OpenExisting(EventName);
            ewh.Set();
            ThreadPool.QueueUserWorkItem(DoStop);
        }

        void DoStop(object unused)
        {
            Stop();
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
        }
    }
}
