using Skipower.Core;
using Skipower.Windows;
using Xunit;

namespace Skipower.Windows.Tests;

public sealed class WindowsPowerControllerTests
{
    [Theory]
    [InlineData(PowerAction.Shutdown, false, false, ShutdownFlags.PowerOff)]
    [InlineData(PowerAction.Restart, false, false, ShutdownFlags.Restart)]
    [InlineData(PowerAction.Shutdown, true, false, ShutdownFlags.PowerOff | ShutdownFlags.ForceSelf)]
    [InlineData(PowerAction.Restart, true, false, ShutdownFlags.Restart | ShutdownFlags.ForceSelf)]
    [InlineData(PowerAction.Shutdown, false, true, ShutdownFlags.PowerOff | ShutdownFlags.Hybrid)]
    public void BuildsOnlyRequestedFlags(PowerAction action, bool force, bool hybrid, uint expected)
    {
        uint actual = WindowsPowerController.GetFlags(new PowerRequest(action, force, hybrid));
        Assert.Equal(expected, actual);
        Assert.Equal(0u, actual & ShutdownFlags.InstallUpdates);
        Assert.Equal(0u, actual & 0x00000001u); // Never force other sessions.
    }

    [Fact]
    public void RejectsHybridRestartBeforeInvokingNativeApi()
    {
        var native = new FakeNativeApi();
        var controller = new WindowsPowerController(native, () => new FakePrivilege(0));
        Assert.Throws<ArgumentException>(() => controller.Execute(new PowerRequest(PowerAction.Restart, Hybrid: true)));
        Assert.Equal(0, native.Calls);
    }

    [Theory]
    [InlineData(0u, PowerRequestStatus.Accepted)]
    [InlineData(5u, PowerRequestStatus.AccessDenied)]
    [InlineData(1191u, PowerRequestStatus.OtherUsersLoggedOn)]
    [InlineData(1115u, PowerRequestStatus.ShutdownAlreadyInProgress)]
    [InlineData(1190u, PowerRequestStatus.ShutdownAlreadyScheduled)]
    [InlineData(21u, PowerRequestStatus.SystemNotReady)]
    [InlineData(9999u, PowerRequestStatus.Failed)]
    public void MapsWin32Results(uint error, PowerRequestStatus expected)
    {
        var native = new FakeNativeApi { Result = error };
        var controller = new WindowsPowerController(native, () => new FakePrivilege(0));
        var result = controller.Execute(new PowerRequest(PowerAction.Shutdown));
        Assert.Equal(expected, result.Status);
        Assert.Equal(1, native.Calls);
        Assert.Equal(ShutdownReasons.PlannedOther, native.Reason);
    }

    [Fact]
    public void MissingShutdownPrivilegePreventsNativeCall()
    {
        var native = new FakeNativeApi();
        var controller = new WindowsPowerController(native, () => new FakePrivilege(WindowsErrors.NotAllAssigned));
        var result = controller.Execute(new PowerRequest(PowerAction.Restart));
        Assert.Equal(PowerRequestStatus.AccessDenied, result.Status);
        Assert.Equal((int)WindowsErrors.NotAllAssigned, result.NativeErrorCode);
        Assert.Equal(0, native.Calls);
    }

    private sealed class FakeNativeApi : IPowerNativeApi
    {
        public uint Result { get; init; }
        public int Calls { get; private set; }
        public uint Reason { get; private set; }

        public uint InitiateShutdown(uint flags, uint reason)
        {
            Calls++;
            Reason = reason;
            Assert.Equal(0u, flags & ShutdownFlags.InstallUpdates);
            return Result;
        }
    }

    private sealed class FakePrivilege(uint errorCode) : IShutdownPrivilege
    {
        public uint ErrorCode => errorCode;
        public void Dispose() { }
    }
}
