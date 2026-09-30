using System.Runtime.InteropServices;

namespace Skipower.Windows;

[StructLayout(LayoutKind.Sequential)]
internal struct Luid
{
    internal uint LowPart;
    internal int HighPart;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LuidAndAttributes
{
    internal Luid Luid;
    internal uint Attributes;
}

[StructLayout(LayoutKind.Sequential)]
internal struct TokenPrivileges
{
    internal uint PrivilegeCount;
    internal LuidAndAttributes Privileges;
}

internal static partial class NativeMethods
{
    [LibraryImport("advapi32.dll", EntryPoint = "InitiateShutdownW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial uint InitiateShutdown(
        string? machineName, string? message, uint gracePeriod, uint shutdownFlags, uint reason);

    [LibraryImport("kernel32.dll")]
    internal static partial nint GetCurrentProcess();

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool OpenProcessToken(nint processHandle, uint desiredAccess, out nint tokenHandle);

    [LibraryImport("advapi32.dll", EntryPoint = "LookupPrivilegeValueW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool LookupPrivilegeValue(string? systemName, string name, out Luid luid);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AdjustTokenPrivileges(
        nint tokenHandle, [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
        ref TokenPrivileges newState, uint bufferLength,
        out TokenPrivileges previousState, out uint returnLength);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseHandle(nint handle);
}
