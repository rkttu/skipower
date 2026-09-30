using System.Runtime.InteropServices;

namespace Skipower.Windows;

internal interface IShutdownPrivilege : IDisposable
{
    uint ErrorCode { get; }
}

internal sealed class ShutdownPrivilege : IShutdownPrivilege
{
    private const uint TokenAdjustPrivileges = 0x0020;
    private const uint TokenQuery = 0x0008;
    private const uint SePrivilegeEnabled = 0x00000002;

    private nint _token;
    private TokenPrivileges _previousState;
    private bool _restore;

    public uint ErrorCode { get; private set; }

    private ShutdownPrivilege() { }

    internal static IShutdownPrivilege Enable()
    {
        var scope = new ShutdownPrivilege();
        if (!NativeMethods.OpenProcessToken(
                NativeMethods.GetCurrentProcess(), TokenAdjustPrivileges | TokenQuery, out scope._token))
        {
            scope.ErrorCode = (uint)Marshal.GetLastPInvokeError();
            return scope;
        }

        if (!NativeMethods.LookupPrivilegeValue(null, "SeShutdownPrivilege", out var luid))
        {
            scope.ErrorCode = (uint)Marshal.GetLastPInvokeError();
            return scope;
        }

        var enabled = new TokenPrivileges
        {
            PrivilegeCount = 1,
            Privileges = new LuidAndAttributes { Luid = luid, Attributes = SePrivilegeEnabled }
        };

        // One privilege can change at most one entry in PreviousState.
        if (!NativeMethods.AdjustTokenPrivileges(
                scope._token, false, ref enabled, (uint)Marshal.SizeOf<TokenPrivileges>(),
                out scope._previousState, out _))
        {
            scope.ErrorCode = (uint)Marshal.GetLastPInvokeError();
            return scope;
        }

        scope.ErrorCode = (uint)Marshal.GetLastPInvokeError();
        scope._restore = scope.ErrorCode == WindowsErrors.Success && scope._previousState.PrivilegeCount != 0;
        return scope;
    }

    public void Dispose()
    {
        if (_token == 0)
            return;

        if (_restore)
        {
            NativeMethods.AdjustTokenPrivileges(
                _token, false, ref _previousState, 0, out _, out _);
        }

        NativeMethods.CloseHandle(_token);
        _token = 0;
    }
}
