namespace Skipower.Windows;

internal static class ShutdownFlags
{
    internal const uint ForceSelf = 0x00000002;
    internal const uint Restart = 0x00000004;
    internal const uint PowerOff = 0x00000008;
    internal const uint InstallUpdates = 0x00000040;
    internal const uint Hybrid = 0x00000200;
}

internal static class ShutdownReasons
{
    // MAJOR_OTHER and MINOR_OTHER are both zero; PLANNED makes the intent explicit.
    internal const uint PlannedOther = 0x80000000;
}

internal static class WindowsErrors
{
    internal const uint Success = 0;
    internal const uint AccessDenied = 5;
    internal const uint NotReady = 21;
    internal const uint NotAllAssigned = 1300;
    internal const uint PrivilegeNotHeld = 1314;
    internal const uint ShutdownInProgress = 1115;
    internal const uint ShutdownIsScheduled = 1190;
    internal const uint ShutdownUsersLoggedOn = 1191;
}
