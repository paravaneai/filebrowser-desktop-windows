using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace FileBrowserDesktop;

internal sealed record StoredCredential(string Username, string Password);

internal static class CredentialManager
{
    private const int CredentialTypeGeneric = 1;
    private const int CredentialPersistLocalMachine = 2;

    public static bool IsSupported => true;

    public static string FileBrowserTargetName(string profileId)
    {
        return $"FileBrowserDesktop/FileBrowser/{profileId}";
    }

    public static StoredCredential? ReadFileBrowserCredential(string profileId)
    {
        return Read(FileBrowserTargetName(profileId));
    }

    public static void WriteFileBrowserCredential(string profileId, string username, string password)
    {
        Write(FileBrowserTargetName(profileId), username, password);
    }

    public static void DeleteFileBrowserCredential(string profileId)
    {
        Delete(FileBrowserTargetName(profileId));
    }

    private static StoredCredential? Read(string targetName)
    {
        if (!CredRead(targetName, CredentialTypeGeneric, 0, out var credentialPointer))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == 1168)
            {
                return null;
            }

            throw new Win32Exception(error, "Could not read Windows Credential Manager entry.");
        }

        try
        {
            var credential = Marshal.PtrToStructure<NativeCredential>(credentialPointer);
            var username = credential.UserName ?? "";
            var password = credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0
                ? ""
                : Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2) ?? "";

            return new StoredCredential(username, password);
        }
        finally
        {
            CredFree(credentialPointer);
        }
    }

    private static void Write(string targetName, string username, string password)
    {
        var passwordBytes = Encoding.Unicode.GetBytes(password);
        var passwordPointer = Marshal.AllocCoTaskMem(passwordBytes.Length);

        try
        {
            Marshal.Copy(passwordBytes, 0, passwordPointer, passwordBytes.Length);

            var credential = new NativeCredential
            {
                TargetName = targetName,
                Type = CredentialTypeGeneric,
                CredentialBlob = passwordPointer,
                CredentialBlobSize = (uint)passwordBytes.Length,
                Persist = CredentialPersistLocalMachine,
                UserName = username,
            };

            if (!CredWrite(ref credential, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not write Windows Credential Manager entry.");
            }
        }
        finally
        {
            if (passwordPointer != IntPtr.Zero)
            {
                Marshal.Copy(new byte[passwordBytes.Length], 0, passwordPointer, passwordBytes.Length);
                Marshal.FreeCoTaskMem(passwordPointer);
            }

            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    private static void Delete(string targetName)
    {
        if (CredDelete(targetName, CredentialTypeGeneric, 0))
        {
            return;
        }

        var error = Marshal.GetLastWin32Error();
        if (error != 1168)
        {
            throw new Win32Exception(error, "Could not delete Windows Credential Manager entry.");
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public int Type;
        public string TargetName;
        public string? Comment;
        public long LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string? UserName;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPointer);

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite(ref NativeCredential credential, int flags);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, int type, int flags);

    [DllImport("advapi32.dll", SetLastError = false)]
    private static extern void CredFree(IntPtr buffer);
}
