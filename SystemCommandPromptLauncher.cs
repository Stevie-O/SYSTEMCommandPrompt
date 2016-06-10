using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace SYSTEMCommandPrompt
{
    class SystemCommandPromptLauncher
    {
        TextWriter _logOutput;

        public SystemCommandPromptLauncher(TextWriter logOutput)
        {
            _logOutput = logOutput;
        }

        void LogMessage(string message)
        {
            _logOutput.WriteLine(message);
        }

        void LogMessage(string format, params object[] args)
        {
            _logOutput.WriteLine(format, args);
        }

        public void LaunchIt()
        {
            Hashtable install_state = new Hashtable();
            Installer uninstaller = null;
            try
            {
                string pid_str = Process.GetCurrentProcess().Id.ToString();
                string service_name = "SYSTEMCommandPrompt_" + pid_str;
                string event_name = "Global\\" + service_name;

                TransactedInstaller master_installer = new TransactedInstaller();

                ServiceInstaller svcinst = new ServiceInstaller();
                // don't set svcinst.Parent
                svcinst.Description = "Temporary service, should be safe to delete.";
                svcinst.DisplayName = "Temporary service (should be safe to delete)";
                svcinst.ServiceName = service_name;
                svcinst.StartType = ServiceStartMode.Manual;

                ServiceProcessInstaller spi = new ServiceProcessInstaller();
                // don't set spi.Parent
                spi.Account = ServiceAccount.LocalSystem;

                master_installer.Installers.AddRange(new Installer[] {
                        spi,
                        svcinst
                    });

                master_installer.Context = new InstallContext();
                master_installer.Context.Parameters["assemblypath"] = string.Format("\"{0}\" -service {1} {2}",
                    typeof(Form1).Assembly.Location,
                    service_name,
                    pid_str);

                LogMessage("Creating service: {0} running as {1}", service_name, spi.Account);

                master_installer.Install(install_state);
                uninstaller = master_installer;

                using (EventWaitHandle ready_signal = new EventWaitHandle(false, EventResetMode.AutoReset, event_name))
                {

                    using (ServiceController sc = new ServiceController(service_name))
                    {
                        LogMessage("Starting service");

                        sc.Start();
                    }

                    LogMessage("Waiting for service to acknowledge...");

                    ready_signal.WaitOne(120000);
                }

            }
            finally
            {
                if (uninstaller != null)
                {
                    LogMessage("Deleting service...");
                    uninstaller.Uninstall(install_state);
                }
            }
        }

    }
}
