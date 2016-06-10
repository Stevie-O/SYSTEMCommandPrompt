using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Windows.Forms;

namespace SYSTEMCommandPrompt
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 3 && args[0] == "-service")
            {
                string service_name = args[1];
                string pid_str = args[2];
                ProcessLauncherSvc svc = new ProcessLauncherSvc();
                svc.AutoLog = false;
                svc.ServiceName = service_name;
                svc.EventName = "Global\\" + service_name;
                svc.Pid = int.Parse(pid_str);
                ServiceBase.Run(svc);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
