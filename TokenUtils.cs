using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;
using System.Threading;
using System.Diagnostics;

namespace MiscUtils
{
    // version 201605060001

    public struct PrivilegeState
    {
        public string PrivilegeName;
        public bool Enabled;

        public PrivilegeState(string name, bool enabled)
        {
            PrivilegeName = name;
            Enabled = enabled;
        }
    }

    public static class TokenPrivileges
    {
        // https://msdn.microsoft.com/en-us/library/windows/desktop/bb530716%28v=vs.85%29.aspx

        /// <summary>
        /// Read any file, bypassing ACLs
        /// </summary>
        public static readonly string SE_BACKUP_NAME = "SeBackupPrivilege";
        /// <summary>
        /// Write any file, bypassing ACLs
        /// </summary>
        public static readonly string SE_RESTORE_NAME = "SeRestorePrivilege";

        /// <summary>
        /// Act as part of the operating system (*grin*)
        /// </summary>
        public static readonly string SE_TCB_NAME = "SeTcbPrivilege";
    }

    // Demand unmanaged code permission to use this class.
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    public sealed class TokenHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Win32 function to unlock the service database.
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);

        // Set ownsHandle to true for the default constructor.
        internal TokenHandle() : base(true) { }

        // Set the handle and set ownsHandle to true.
        internal TokenHandle(IntPtr preexistingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        // Perform any specific actions to release the 
        // handle in the ReleaseHandle method.
        // Often, you need to use Pinvoke to make
        // a call into the Win32 API to release the 
        // handle. In this case, however, we can use
        // the Marshal class to release the unmanaged
        // memory.
        override protected bool ReleaseHandle()
        {
            // "handle" is the internal
            // value for the IntPtr handle.

            // If the handle was set,
            // free it. Return success.
            IntPtr oldHandle = Interlocked.Exchange(ref handle, IntPtr.Zero);
            if (oldHandle != IntPtr.Zero)
            {
                // Free the handle.
                if (!CloseHandle(oldHandle))
                    return false;

            }

            // Return success.
            return true;
        }

        // useful methods/properties

        public bool IsElevated { get { return TokenUtil.GetTokenElevated(this); } }
        public TokenElevationType ElevationType { get { return TokenUtil.GetTokenElevationType(this); } }
        public TokenHandle GetLinkedToken() { return TokenUtil.GetLinkedToken(this); }
        public TokenType TokenType { get { return TokenUtil.GetTokenType(this); } }
        public SECURITY_IMPERSONATION_LEVEL ImpersonationLevel { get { return TokenUtil.GetTokenImpersonationLevel(this); } }
        public int SessionId
        {
            get { return TokenUtil.GetTokenInt(this, TOKEN_INFORMATION_CLASS.TokenSessionId); }
            set { TokenUtil.SetTokenInt(this, TOKEN_INFORMATION_CLASS.TokenSessionId, value); }
        }

        public bool DisableAllPrivileges()
        {
            return TokenUtil.AdjustTokenPrivileges(this, true, null);
        }
        public bool AdjustPrivileges(PrivilegeState[] privileges)
        {
            return TokenUtil.AdjustTokenPrivileges(this, false, privileges);
        }
    } // class TokenHandle

    public enum TokenType
    {
        TokenPrimary = 1,
        TokenImpersonation,
    }

    public enum SECURITY_IMPERSONATION_LEVEL
    {
        SecurityAnonymous,
        SecurityIdentification,
        SecurityImpersonation,
        SecurityDelegation
    }

    public static class StandardRights
    {
        ///<summary>
        ///#define DELETE (0x00010000L)
        ///</summary>
        public const int DELETE = (0x00010000);
        ///<summary>
        ///#define READ_CONTROL (0x00020000L)
        ///</summary>
        public const int READ_CONTROL = (0x00020000);
        ///<summary>
        ///#define WRITE_DAC (0x00040000L)
        ///</summary>
        public const int WRITE_DAC = (0x00040000);
        ///<summary>
        ///#define WRITE_OWNER (0x00080000L)
        ///</summary>
        public const int WRITE_OWNER = (0x00080000);
        ///<summary>
        ///#define SYNCHRONIZE (0x00100000L)
        ///</summary>
        public const int SYNCHRONIZE = (0x00100000);
        ///<summary>
        ///#define STANDARD_RIGHTS_REQUIRED (0x000F0000L)
        ///</summary>
        public const int STANDARD_RIGHTS_REQUIRED = (0x000F0000);
        ///<summary>
        ///#define STANDARD_RIGHTS_READ (READ_CONTROL)
        ///</summary>
        public const int STANDARD_RIGHTS_READ = (READ_CONTROL);
        ///<summary>
        ///#define STANDARD_RIGHTS_WRITE (READ_CONTROL)
        ///</summary>
        public const int STANDARD_RIGHTS_WRITE = (READ_CONTROL);
        ///<summary>
        ///#define STANDARD_RIGHTS_EXECUTE (READ_CONTROL)
        ///</summary>
        public const int STANDARD_RIGHTS_EXECUTE = (READ_CONTROL);
        ///<summary>
        ///#define STANDARD_RIGHTS_ALL (0x001F0000L)
        ///</summary>
        public const int STANDARD_RIGHTS_ALL = (0x001F0000);
        ///<summary>
        ///#define SPECIFIC_RIGHTS_ALL (0x0000FFFFL)
        ///</summary>
        public const int SPECIFIC_RIGHTS_ALL = (0x0000FFFF);
        ///<summary>
        ///
        /// AccessSystemAcl access type
        ///
        ///
        ///#define ACCESS_SYSTEM_SECURITY (0x01000000L)
        ///</summary>
        public const int ACCESS_SYSTEM_SECURITY = (0x01000000);
        ///<summary>
        ///
        /// MaximumAllowed access type
        ///
        ///
        ///#define MAXIMUM_ALLOWED (0x02000000L)
        ///</summary>
        public const int MAXIMUM_ALLOWED = (0x02000000);
        ///<summary>
        ///
        /// These are the generic rights.
        ///
        ///
        ///#define GENERIC_READ (0x80000000L)
        ///</summary>
        public const int GENERIC_READ = unchecked((int)(0x80000000));
        ///<summary>
        ///#define GENERIC_WRITE (0x40000000L)
        ///</summary>
        public const int GENERIC_WRITE = (0x40000000);
        ///<summary>
        ///#define GENERIC_EXECUTE (0x20000000L)
        ///</summary>
        public const int GENERIC_EXECUTE = (0x20000000);
        ///<summary>
        ///#define GENERIC_ALL (0x10000000L)
        ///</summary>
        public const int GENERIC_ALL = (0x10000000);
    }

    public enum TokenAccess
    {
        ///<summary>
        ///#define TOKEN_ASSIGN_PRIMARY (0x0001)
        ///</summary>
        TOKEN_ASSIGN_PRIMARY = (0x0001),
        ///<summary>
        ///#define TOKEN_DUPLICATE (0x0002)
        ///</summary>
        TOKEN_DUPLICATE = (0x0002),
        ///<summary>
        ///#define TOKEN_IMPERSONATE (0x0004)
        ///</summary>
        TOKEN_IMPERSONATE = (0x0004),
        ///<summary>
        ///#define TOKEN_QUERY (0x0008)
        ///</summary>
        TOKEN_QUERY = (0x0008),
        ///<summary>
        ///#define TOKEN_QUERY_SOURCE (0x0010)
        ///</summary>
        TOKEN_QUERY_SOURCE = (0x0010),
        ///<summary>
        ///#define TOKEN_ADJUST_PRIVILEGES (0x0020)
        ///</summary>
        TOKEN_ADJUST_PRIVILEGES = (0x0020),
        ///<summary>
        ///#define TOKEN_ADJUST_GROUPS (0x0040)
        ///</summary>
        TOKEN_ADJUST_GROUPS = (0x0040),
        ///<summary>
        ///#define TOKEN_ADJUST_DEFAULT (0x0080)
        ///</summary>
        TOKEN_ADJUST_DEFAULT = (0x0080),
        ///<summary>
        ///#define TOKEN_ADJUST_SESSIONID (0x0100)
        ///</summary>
        TOKEN_ADJUST_SESSIONID = (0x0100),
        ///<summary>
        ///#define TOKEN_ALL_ACCESS_P (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT )
        ///</summary>
        TOKEN_ALL_ACCESS_P = (StandardRights.STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT),
        ///<summary>
        ///#define TOKEN_ALL_ACCESS (TOKEN_ALL_ACCESS_P | TOKEN_ADJUST_SESSIONID )
        ///</summary>
        TOKEN_ALL_ACCESS = (TOKEN_ALL_ACCESS_P | TOKEN_ADJUST_SESSIONID),
        ///<summary>
        ///#define TOKEN_READ (STANDARD_RIGHTS_READ | TOKEN_QUERY)
        ///</summary>
        TOKEN_READ = (StandardRights.STANDARD_RIGHTS_READ | TOKEN_QUERY),
        ///<summary>
        ///#define TOKEN_WRITE (STANDARD_RIGHTS_WRITE | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT)
        ///</summary>
        TOKEN_WRITE = (StandardRights.STANDARD_RIGHTS_WRITE | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT),
        ///<summary>
        ///#define TOKEN_EXECUTE (STANDARD_RIGHTS_EXECUTE)
        ///</summary>
        TOKEN_EXECUTE = (StandardRights.STANDARD_RIGHTS_EXECUTE),
    }

    public enum TokenElevationType
    {
        /// <summary>
        /// UAC virtualization is not currently in effect (either user is not an admin, or UAC is turned off for the machine)
        /// </summary>
        Default = 1,
        /// <summary>
        /// UAC virtualization is in effect and the user is elevated
        /// </summary>
        Full = 2,
        /// <summary>
        /// UAC virtualization is in effect and the user is not elevated
        /// </summary>
        Limited = 3
    }

    public enum TOKEN_INFORMATION_CLASS
    {
        TokenUser = 1,
        TokenGroups = 2,
        TokenPrivileges = 3,
        TokenOwner = 4,
        TokenPrimaryGroup = 5,
        TokenDefaultDacl = 6,
        TokenSource = 7,
        TokenType = 8,
        TokenImpersonationLevel = 9,
        TokenStatistics = 10,
        TokenRestrictedSids = 11,
        TokenSessionId = 12,
        TokenGroupsAndPrivileges = 13,
        TokenSessionReference = 14,
        TokenSandBoxInert = 15,
        TokenAuditPolicy = 16,
        TokenOrigin = 17,
        TokenElevationType = 18,
        TokenLinkedToken = 19,
        TokenElevation = 20,
        TokenHasRestrictions = 21,
        TokenAccessInformation = 22,
        TokenVirtualizationAllowed = 23,
        TokenVirtualizationEnabled = 24,
        TokenIntegrityLevel = 25,
        TokenUIAccess = 26,
        TokenMandatoryPolicy = 27,
        TokenLogonSid = 28,
    }

    public enum TOKEN_TYPE
    {
        TokenPrimary = 1,
        TokenImpersonation,
    }

    /// <summary>
    /// PInvoke stuff for process/thread token functions (almost all of which are in advapi32.dll)
    /// </summary>
    public static class TokenUtil
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, out TokenHandle TokenHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        extern static bool DuplicateTokenEx(SafeHandle hExistingToken, uint dwDesiredAccess,
                IntPtr lpTokenAttributes, SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, TOKEN_TYPE TokenType, out TokenHandle phNewToken);


        public static TokenHandle OpenProcessToken(Process process, TokenAccess desiredAccess)
        {
            return OpenProcessToken(process.Handle, desiredAccess);
        }

        public static TokenHandle OpenProcessToken(IntPtr hProcess, TokenAccess desiredAccess)
        {
            TokenHandle ret;
            bool success = OpenProcessToken(hProcess, (int)desiredAccess, out ret);
            if (!success) throw new Win32Exception();
            return ret;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetTokenInformation(TokenHandle TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, int TokenInformationLength, out int ReturnLength);

        struct TOKEN_ELEVATION
        {
            public int TokenIsElevated;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetTokenInformation(TokenHandle TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, ref TOKEN_ELEVATION TokenInformation, int TokenInformationLength, out int ReturnLength);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetTokenInformation(TokenHandle TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, ref int TokenInformation, int TokenInformationLength, out int ReturnLength);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetTokenInformation(TokenHandle TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, ref IntPtr TokenInformation, int TokenInformationLength, out int ReturnLength);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetTokenInformation(TokenHandle TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, [In] ref int TokenInformation, int TokenInformationLength);

        public static bool GetTokenElevated(TokenHandle hToken)
        {
            int size;
            TOKEN_ELEVATION te = new TOKEN_ELEVATION();
            te.TokenIsElevated = 0;
            bool result = GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenElevation, ref te, Marshal.SizeOf(typeof(TOKEN_ELEVATION)), out size);
            if (!result) throw new Win32Exception();
            return te.TokenIsElevated != 0;
        }

        public static int GetTokenInt(TokenHandle hToken, TOKEN_INFORMATION_CLASS infoClass)
        {
            int size;
            int value = 0;
            bool result = GetTokenInformation(hToken, infoClass, ref value, 4, out size);
            if (!result) throw new Win32Exception();
            return value;
        }

        public static void SetTokenInt(TokenHandle hToken, TOKEN_INFORMATION_CLASS infoClass, int value)
        {
            bool result = SetTokenInformation(hToken, infoClass, ref value, 4);
            if (!result) throw new Win32Exception();
        }

        public static TokenElevationType GetTokenElevationType(TokenHandle hToken)
        {
            return (TokenElevationType)GetTokenInt(hToken, TOKEN_INFORMATION_CLASS.TokenElevationType);
        }

        public static TokenType GetTokenType(TokenHandle hToken)
        {
            return (TokenType)GetTokenInt(hToken, TOKEN_INFORMATION_CLASS.TokenType);
        }

        public static SECURITY_IMPERSONATION_LEVEL GetTokenImpersonationLevel(TokenHandle hToken)
        {
            return (SECURITY_IMPERSONATION_LEVEL)GetTokenInt(hToken, TOKEN_INFORMATION_CLASS.TokenImpersonationLevel);
        }

        // Win32 function to unlock the service database.
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);

        const int ERROR_NO_SUCH_LOGON_SESSION = 1312;

        public static TokenHandle GetLinkedToken(TokenHandle hToken)
        {
            int size;
            IntPtr handle = IntPtr.Zero;
            bool result = GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenLinkedToken, ref handle, IntPtr.Size, out size);
            int err = Marshal.GetLastWin32Error();
            if (!result)
            {
                // this happens if UAC virtualization is not enabled for the token
                // *that* happens if UAC is disabled for the system, *or* if the user a regular non-admin user.
                if (err == ERROR_NO_SUCH_LOGON_SESSION) return null;
                throw new Win32Exception(err);
            }
            return new TokenHandle(handle, true);
        }

        const int SE_PRIVILEGE_DISABLED = 0x00000000;
        const int SE_PRIVILEGE_ENABLED = 0x00000002;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct LUID
        {
            public int LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public int Attributes;
        }

        class TokenPrivilegesMarshaler : ICustomMarshaler
        {
            static TokenPrivilegesMarshaler static_instance;

            public unsafe static IntPtr MarshalManagedToNative(LUID_AND_ATTRIBUTES[] attributes)
            {
                if (attributes == null) return IntPtr.Zero;

                int total_size = 4 + attributes.Length * Marshal.SizeOf(typeof(LUID_AND_ATTRIBUTES));

                IntPtr pNativeData = Marshal.AllocHGlobal(total_size);
                Marshal.WriteInt32(pNativeData, attributes.Length);
                IntPtr pArrayData = new IntPtr(pNativeData.ToInt64() + 4);

                LUID_AND_ATTRIBUTES* dst = (LUID_AND_ATTRIBUTES*)pArrayData.ToPointer();
                fixed (LUID_AND_ATTRIBUTES* src_base = attributes)
                {
                    LUID_AND_ATTRIBUTES* src = src_base;
                    for (int i = 0; i < attributes.Length; i++)
                    {
                        *dst = *src;
                        dst++;
                        src++;
                    }
                }

                return pNativeData;

            }

            public unsafe static LUID_AND_ATTRIBUTES[] MarshalFromIntPtr(IntPtr pNativeData)
            {
                if (pNativeData == IntPtr.Zero) return null;
                int count = Marshal.ReadInt32(pNativeData);
                LUID_AND_ATTRIBUTES[] ary = new LUID_AND_ATTRIBUTES[count];

                IntPtr pArrayData = new IntPtr(pNativeData.ToInt64() + 4);

                LUID_AND_ATTRIBUTES* src = (LUID_AND_ATTRIBUTES*)pArrayData.ToPointer();
                fixed (LUID_AND_ATTRIBUTES* dst_base = ary)
                {
                    LUID_AND_ATTRIBUTES* dst = dst_base;
                    for (int i = 0; i < count; i++)
                    {
                        *dst = *src;
                        dst++;
                        src++;
                    }
                }

                return ary;
            }

            public unsafe IntPtr MarshalManagedToNative(object managedObj)
            {
                if (managedObj == null)
                    return IntPtr.Zero;

                LUID_AND_ATTRIBUTES[] obj = managedObj as LUID_AND_ATTRIBUTES[];
                if (obj == null)
                    throw new MarshalDirectiveException("TokenPrivilegesMarshaler must be used on an array of LUID_AND_ATTRIBUTES structures.");

                return MarshalManagedToNative(obj);
            }

            public unsafe object MarshalNativeToManaged(IntPtr pNativeData)
            {
                return MarshalFromIntPtr(pNativeData);
            }

            public void CleanUpNativeData(IntPtr pNativeData)
            {
                Marshal.FreeHGlobal(pNativeData);
            }

            public void CleanUpManagedData(object managedObj)
            {
            }

            public int GetNativeDataSize()
            {
                return -1;
            }

            public static ICustomMarshaler GetInstance(string cookie)
            {
                if (static_instance == null)
                {
                    return static_instance = new TokenPrivilegesMarshaler();
                }
                return static_instance;
            }

        }

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool AdjustTokenPrivileges(TokenHandle hToken, bool disableAllPrivileges,
                [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(TokenPrivilegesMarshaler))]
                LUID_AND_ATTRIBUTES[] NewState,
                int BufferLength,
                IntPtr PreviousState,
                out int returnLength);

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool LookupPrivilegeName(string lpSystemName, [In] ref LUID luid_and_attr, StringBuilder lpName, ref int cchName);

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        static LUID_AND_ATTRIBUTES[] ConvertTokenPrivileges(PrivilegeState[] state)
        {
            if (state == null) return null;
            LUID_AND_ATTRIBUTES[] conv = new LUID_AND_ATTRIBUTES[state.Length];
            for (int i = 0; i < state.Length; i++)
            {
                string name = state[i].PrivilegeName;
                bool enabled = state[i].Enabled;

                if (name == null) throw new InvalidOperationException("Null PrivilegeName at index " + i.ToString());

                if (!LookupPrivilegeValue(null, name, out conv[i].Luid))
                    throw new Win32Exception();

                conv[i].Attributes = (enabled ? SE_PRIVILEGE_ENABLED : SE_PRIVILEGE_DISABLED);
            }
            return conv;
        }

        public static bool AdjustTokenPrivileges(TokenHandle hToken, bool disableAllPrivileges, PrivilegeState[] newState)
        {
            int unused;
            LUID_AND_ATTRIBUTES[] newState_conv = ConvertTokenPrivileges(newState);
            bool result = AdjustTokenPrivileges(hToken, disableAllPrivileges, newState_conv, 0, IntPtr.Zero, out unused);
            if (!result) throw new Win32Exception();
            return result;
        }

        public static TokenHandle DuplicateTokenEx(TokenHandle hExistingToken, TokenAccess desiredAccess,
                SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, TOKEN_TYPE TokenType)
        {
            TokenHandle ret;
            bool result = DuplicateTokenEx(hExistingToken, (uint)desiredAccess, IntPtr.Zero, ImpersonationLevel, TokenType, out ret);
            if (!result) throw new Win32Exception();
            return ret;
        }


    }
}
