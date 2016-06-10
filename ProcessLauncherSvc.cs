using MiscUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
            try
            {
                using (TokenHandle my_token = TokenUtil.OpenProcessToken(Process.GetCurrentProcess(), TokenAccess.TOKEN_ADJUST_PRIVILEGES | TokenAccess.TOKEN_DUPLICATE))
                {
                    PrivilegeState[] privs = new PrivilegeState[] {
                            new PrivilegeState(TokenPrivileges.SE_TCB_NAME, true)
                    };
                    if (!my_token.AdjustPrivileges(privs))
                        throw new Win32Exception();

                    // open the token of the process that lives in the session we want
                    Process proc = Process.GetProcessById(this.Pid);
                    TokenHandle proc_token = TokenUtil.OpenProcessToken(proc.Handle, TokenAccess.TOKEN_READ);

                    // create a primary token
                    TokenHandle new_token = TokenUtil.DuplicateTokenEx(my_token, TokenAccess.TOKEN_ALL_ACCESS, SECURITY_IMPERSONATION_LEVEL.SecurityDelegation, TOKEN_TYPE.TokenPrimary);
                    // override the session under which it's going to be created
                    new_token.SessionId = proc_token.SessionId;

                    string cmd_exe = Environment.GetEnvironmentVariable("COMSPEC");

                    STARTUPINFO sui = new STARTUPINFO();
                    sui.cb = Marshal.SizeOf(typeof(STARTUPINFO));

                    PROCESS_INFORMATION procinfo;

                    bool result = Advapi32.CreateProcessAsUser(
                            new_token,
                            cmd_exe,
                            cmd_exe,
                            IntPtr.Zero, // default process attributes
                            IntPtr.Zero, // default thread attributes
                            false,
                            CreateProcessFlags.CREATE_NEW_CONSOLE | CreateProcessFlags.CREATE_NEW_PROCESS_GROUP | CreateProcessFlags.CREATE_DEFAULT_ERROR_MODE | CreateProcessFlags.CREATE_BREAKAWAY_FROM_JOB | CreateProcessFlags.CREATE_PRESERVE_CODE_AUTHZ_LEVEL | CreateProcessFlags.CREATE_UNICODE_ENVIRONMENT,
                            IntPtr.Zero, // inherit environment
                            null, // inherit current directory
                            ref sui,
                            out procinfo);
                    if (!result)
                        throw new Win32Exception();
                }
            }
            catch
            {
                this.ExitCode = 1;
            }
            finally
            {
                EventWaitHandle ewh = EventWaitHandle.OpenExisting(EventName);
                ewh.Set();
            }
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
