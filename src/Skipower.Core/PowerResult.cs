namespace Skipower.Core;

public enum PowerRequestStatus
{
    Accepted,
    AccessDenied,
    OtherUsersLoggedOn,
    ShutdownAlreadyInProgress,
    ShutdownAlreadyScheduled,
    SystemNotReady,
    Failed
}

public sealed record PowerResult(
    PowerRequest Request,
    PowerRequestStatus Status,
    int? NativeErrorCode = null);
