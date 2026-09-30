using System.Globalization;
using Skipower.Cli;
using Skipower.Core;
using Skipower.Windows;
using Xunit;

namespace Skipower.Core.Tests;

public sealed class CliRunnerTests
{
    [Fact]
    public void HelpAndInvalidOptionsNeverInvokePowerController()
    {
        var fake = new FakeController(PowerRequestStatus.Accepted);
        Assert.Equal(0, Run(["--help"], fake).Code);
        Assert.Equal(2, Run(["restart", "--hybrid"], fake).Code);
        Assert.Equal(2, Run(["shutdown", "--force", "--force"], fake).Code);
        Assert.Equal(2, Run(["shutdown", "--unknown"], fake).Code);
        Assert.Equal(2, Run(["shutdown", "--elevate", "--elevated-child"], fake).Code);
        Assert.Empty(fake.Requests);
    }

    [Fact]
    public void RestartJsonReportsAcceptedRequestAndNoUpdateInstallRequest()
    {
        var fake = new FakeController(PowerRequestStatus.Accepted);
        var result = Run(["restart", "--json", "--verbose"], fake);

        Assert.Equal(0, result.Code);
        Assert.Equal(new PowerRequest(PowerAction.Restart), Assert.Single(fake.Requests));
        Assert.Contains("\"installUpdatesRequested\":false", result.Output);
        Assert.Contains("\"status\":\"accepted\"", result.Output);
    }

    [Theory]
    [InlineData(PowerRequestStatus.AccessDenied, 10)]
    [InlineData(PowerRequestStatus.OtherUsersLoggedOn, 11)]
    [InlineData(PowerRequestStatus.ShutdownAlreadyInProgress, 12)]
    [InlineData(PowerRequestStatus.ShutdownAlreadyScheduled, 13)]
    [InlineData(PowerRequestStatus.SystemNotReady, 14)]
    [InlineData(PowerRequestStatus.Failed, 30)]
    public void FailureMapsToStableExitCode(PowerRequestStatus status, int expectedCode)
    {
        var result = Run(["shutdown", "--force", "--hybrid", "--json"], new FakeController(status));
        Assert.Equal(expectedCode, result.Code);
        Assert.Contains("\"installUpdatesRequested\":false", result.Output);
    }

    [Fact]
    public void ElevationIsOfferedOnlyAfterAccessDenied()
    {
        var launcher = new FakeLauncher(new ElevationLaunchResult(ElevationLaunchStatus.Started));
        var accepted = Run(["restart", "--elevate"], new FakeController(PowerRequestStatus.Accepted), launcher);
        Assert.Equal(0, accepted.Code);
        Assert.Equal(0, launcher.Calls);

        launcher.Result = new ElevationLaunchResult(
            ElevationLaunchStatus.Started,
            ElevatedExitCode.Encode(new PowerResult(new PowerRequest(PowerAction.Restart), PowerRequestStatus.Accepted)));
        var retried = Run(["restart", "--elevate", "--json"], new FakeController(PowerRequestStatus.AccessDenied), launcher);
        Assert.Equal(0, retried.Code);
        Assert.Equal(1, launcher.Calls);
        Assert.Equal(["restart", "--elevated-child"], launcher.Arguments);
        Assert.True(launcher.WaitForExit);
        Assert.True(launcher.HideWindow);
        Assert.Contains("\"status\":\"accepted\"", retried.Output);
    }

    [Fact]
    public void CancelledElevationReturnsWithoutSecondPowerRequest()
    {
        var controller = new FakeController(PowerRequestStatus.AccessDenied);
        var launcher = new FakeLauncher(new ElevationLaunchResult(ElevationLaunchStatus.Cancelled, NativeErrorCode: 1223));
        var result = Run(["shutdown", "--elevate", "--json"], controller, launcher);
        Assert.Equal(10, result.Code);
        Assert.Single(controller.Requests);
        Assert.Contains("\"status\":\"elevationCancelled\"", result.Output);
        Assert.Contains("\"nativeErrorCode\":1223", result.Output);
    }

    [Fact]
    public void ElevatedRetryPreservesPowerFailureAndExitCode()
    {
        var request = new PowerRequest(PowerAction.Shutdown, Force: true, Hybrid: true);
        var launcher = new FakeLauncher(new ElevationLaunchResult(
            ElevationLaunchStatus.Started,
            ElevatedExitCode.Encode(new PowerResult(request, PowerRequestStatus.OtherUsersLoggedOn, 1191))));
        var result = Run(["shutdown", "--force", "--hybrid", "--elevate", "--json"],
            new FakeController(PowerRequestStatus.AccessDenied), launcher);

        Assert.Equal(11, result.Code);
        Assert.Equal(["shutdown", "--force", "--hybrid", "--elevated-child"], launcher.Arguments);
        Assert.Contains("\"status\":\"otherUsersLoggedOn\"", result.Output);
        Assert.Contains("\"nativeErrorCode\":1191", result.Output);
    }

    [Fact]
    public void ElevatedProcessFailureDoesNotLookLikeAcceptedRequest()
    {
        var launcher = new FakeLauncher(new ElevationLaunchResult(ElevationLaunchStatus.Started, ExitCode: 1));
        var result = Run(["restart", "--elevate", "--json"],
            new FakeController(PowerRequestStatus.AccessDenied), launcher);
        Assert.Equal(30, result.Code);
        Assert.Contains("\"status\":\"elevationFailed\"", result.Output);
    }

    [Fact]
    public void ElevatedChildReturnsResultWithoutWritingConsoleOutput()
    {
        var result = Run(["shutdown", "--force", "--elevated-child"], new FakeController(PowerRequestStatus.OtherUsersLoggedOn));
        Assert.Empty(result.Output);
        Assert.Empty(result.Error);
        Assert.True(ElevatedExitCode.TryDecode(
            new PowerRequest(PowerAction.Shutdown, Force: true), result.Code, out var decoded));
        Assert.Equal(PowerRequestStatus.OtherUsersLoggedOn, decoded.Status);
        Assert.Equal(5, decoded.NativeErrorCode);
    }

    [Theory]
    [InlineData("ko-KR", "사용법:", "종료 요청을 Windows가 접수했습니다.")]
    [InlineData("en-US", "Usage:", "Shutdown request accepted.")]
    [InlineData("fr-FR", "Usage:", "Shutdown request accepted.")]
    public void UsesWindowsUiLanguageWithEnglishFallback(string cultureName, string helpPrefix, string successMessage)
    {
        UiStrings strings = UiStrings.ForCulture(CultureInfo.GetCultureInfo(cultureName));
        Assert.StartsWith(helpPrefix, Run(["--help"], new FakeController(PowerRequestStatus.Accepted), strings: strings).Output);
        Assert.Contains(successMessage, Run(["shutdown"], new FakeController(PowerRequestStatus.Accepted), strings: strings).Output);
    }

    [Fact]
    public void EnglishAndKoreanHaveEveryDisplayString()
    {
        foreach (MessageId id in Enum.GetValues<MessageId>())
        {
            Assert.NotEmpty(UiStrings.ForLanguage("en")[id]);
            Assert.NotEmpty(UiStrings.ForLanguage("ko")[id]);
        }
    }

    private static (int Code, string Output, string Error) Run(
        string[] args, FakeController fake, IElevatedProcessLauncher? launcher = null, UiStrings? strings = null)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        int code = CliRunner.Run(args, fake, output, error, launcher, strings);
        return (code, output.ToString(), error.ToString());
    }

    private sealed class FakeLauncher(ElevationLaunchResult result) : IElevatedProcessLauncher
    {
        public ElevationLaunchResult Result { get; set; } = result;
        public int Calls { get; private set; }
        public string[] Arguments { get; private set; } = [];
        public bool WaitForExit { get; private set; }
        public bool HideWindow { get; private set; }

        public ElevationLaunchResult Launch(IReadOnlyList<string> arguments, bool waitForExit, bool hideWindow)
        {
            Calls++;
            Arguments = [.. arguments];
            WaitForExit = waitForExit;
            HideWindow = hideWindow;
            return Result;
        }
    }

    private sealed class FakeController(PowerRequestStatus status) : IPowerController
    {
        public List<PowerRequest> Requests { get; } = [];

        public PowerResult Execute(PowerRequest request, Action<PowerDiagnostic>? diagnostic = null)
        {
            Requests.Add(request);
            return new PowerResult(request, status, status == PowerRequestStatus.Accepted ? null : 5);
        }
    }
}
