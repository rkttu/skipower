namespace Skipower.Core;

internal static class EnglishStrings
{
    internal static string Get(MessageId id) => id switch
    {
        MessageId.Usage => "Usage: skipower <shutdown|restart> [--force] [--hybrid] [--json] [--verbose] [--elevate]\n       skipower --help\n       skipower --version",
        MessageId.InvalidCommand => "Invalid command line.",
        MessageId.ShutdownAccepted => "Shutdown request accepted.",
        MessageId.RestartAccepted => "Restart request accepted.",
        MessageId.InstallUpdatesNotRequested => "Windows Update installation was not requested.",
        MessageId.UpdateMayStillComplete => "An update already awaiting restart or an organization policy may still complete it.",
        MessageId.ShutdownNotRequested => "Shutdown was not requested.",
        MessageId.RestartNotRequested => "Restart was not requested.",
        MessageId.AccessDenied => "Shutdown privilege is unavailable under the current security policy.",
        MessageId.OtherUsersLoggedOn => "Another user is currently signed in.",
        MessageId.ShutdownInProgress => "A shutdown is already in progress.",
        MessageId.ShutdownScheduled => "A shutdown is already scheduled.",
        MessageId.SystemNotReady => "Windows is not ready to accept the request.",
        MessageId.WindowsError => "Windows error code: {0}.",
        MessageId.Unknown => "unknown",
        MessageId.ElevationCancelled => "Administrator approval was canceled. The request was not retried.",
        MessageId.ElevationLaunchFailed => "Could not start the administrator retry. Windows error code: {0}.",
        MessageId.ElevationUnexpectedExit => "The administrator retry ended without a result. Process exit code: {0}.",
        MessageId.DiagnosticRequest => "Action: {0}; native flags: 0x{1:X8}",
        MessageId.DiagnosticPrivilege => "Privilege activation Windows code: {0}",
        MessageId.DiagnosticShutdown => "InitiateShutdownW Windows code: {0}",
        MessageId.DiagnosticElevatedExit => "Administrator retry process exit code: {0}",
        MessageId.GuiLead => "Run a power action without requesting Windows Update installation.",
        MessageId.GuiScope => "An update awaiting restart or an organization policy may still complete an update.",
        MessageId.GuiShutdown => "Shut down",
        MessageId.GuiShutdownDescription => "Turn off this PC without requesting update installation.",
        MessageId.GuiRestart => "Restart",
        MessageId.GuiRestartDescription => "Restart this PC without requesting update installation.",
        MessageId.GuiCancel => "Cancel",
        MessageId.GuiClose => "Close",
        MessageId.GuiElevateRetry => "Retry as administrator",
        MessageId.GuiAccepted => "Windows accepted the request. Update installation was not requested.",
        MessageId.GuiAccessDenied => "Shutdown privilege is unavailable. You can retry with administrator approval.",
        MessageId.GuiAccessDeniedElevated => "Shutdown privilege is unavailable under the current security policy.",
        MessageId.GuiOtherUsers => "Another user is signed in. Windows did not accept the request.",
        MessageId.GuiInProgress => "A shutdown or restart is already in progress.",
        MessageId.GuiScheduled => "Another shutdown is already scheduled.",
        MessageId.GuiNotReady => "Windows is not ready to accept the request.",
        MessageId.GuiFailed => "The request failed. Windows error code: {0}.",
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };
}
