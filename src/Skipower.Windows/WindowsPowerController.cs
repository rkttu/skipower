using Skipower.Core;

namespace Skipower.Windows;

internal interface IPowerNativeApi
{
    uint InitiateShutdown(uint flags, uint reason);
}

internal sealed class PowerNativeApi : IPowerNativeApi
{
    public uint InitiateShutdown(uint flags, uint reason) =>
        NativeMethods.InitiateShutdown(null, null, 0, flags, reason);
}

public sealed class WindowsPowerController : IPowerController
{
    private readonly IPowerNativeApi _native;
    private readonly Func<IShutdownPrivilege> _enablePrivilege;

    public WindowsPowerController() : this(new PowerNativeApi(), ShutdownPrivilege.Enable) { }

    internal WindowsPowerController(IPowerNativeApi native, Func<IShutdownPrivilege> enablePrivilege)
    {
        _native = native;
        _enablePrivilege = enablePrivilege;
    }

    public PowerResult Execute(PowerRequest request, Action<PowerDiagnostic>? diagnostic = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        var flags = GetFlags(request);
        diagnostic?.Invoke(new PowerDiagnostic(PowerDiagnosticStage.Request, NativeFlags: flags));

        using var privilege = _enablePrivilege();
        diagnostic?.Invoke(new PowerDiagnostic(PowerDiagnosticStage.PrivilegeActivation, NativeCode: privilege.ErrorCode));
        if (privilege.ErrorCode != WindowsErrors.Success)
        {
            var error = privilege.ErrorCode is WindowsErrors.NotAllAssigned or WindowsErrors.PrivilegeNotHeld
                ? WindowsErrors.AccessDenied : privilege.ErrorCode;
            return new PowerResult(request, MapError(error), (int)privilege.ErrorCode);
        }

        uint result = _native.InitiateShutdown(flags, ShutdownReasons.PlannedOther);
        diagnostic?.Invoke(new PowerDiagnostic(PowerDiagnosticStage.ShutdownRequest, NativeCode: result));
        return new PowerResult(request, MapError(result), result == 0 ? null : (int)result);
    }

    internal static uint GetFlags(PowerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Hybrid && request.Action != PowerAction.Shutdown)
            throw new ArgumentException("Hybrid shutdown is only valid for shutdown.", nameof(request));

        uint flags = request.Action switch
        {
            PowerAction.Shutdown => ShutdownFlags.PowerOff,
            PowerAction.Restart => ShutdownFlags.Restart,
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        if (request.Force)
            flags |= ShutdownFlags.ForceSelf;
        if (request.Hybrid)
            flags |= ShutdownFlags.Hybrid;
        return flags;
    }

    internal static PowerRequestStatus MapError(uint error) => error switch
    {
        WindowsErrors.Success => PowerRequestStatus.Accepted,
        WindowsErrors.AccessDenied or WindowsErrors.PrivilegeNotHeld or WindowsErrors.NotAllAssigned => PowerRequestStatus.AccessDenied,
        WindowsErrors.ShutdownUsersLoggedOn => PowerRequestStatus.OtherUsersLoggedOn,
        WindowsErrors.ShutdownInProgress => PowerRequestStatus.ShutdownAlreadyInProgress,
        WindowsErrors.ShutdownIsScheduled => PowerRequestStatus.ShutdownAlreadyScheduled,
        WindowsErrors.NotReady => PowerRequestStatus.SystemNotReady,
        _ => PowerRequestStatus.Failed
    };
}
