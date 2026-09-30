using System.Globalization;
using System.Reflection;
using Skipower.Core;
using Skipower.Windows;

namespace Skipower.Cli;

public static class CliRunner
{
    public static int Run(
        string[] args,
        IPowerController controller,
        TextWriter output,
        TextWriter error,
        IElevatedProcessLauncher? launcher = null,
        UiStrings? strings = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        strings ??= UiStrings.ForCurrentCulture();

        if (args.Length == 1 && args[0] is "--help" or "-h")
        {
            output.WriteLine(strings[MessageId.Usage]);
            return 0;
        }

        if (args.Length == 1 && args[0] == "--version")
        {
            output.WriteLine(typeof(CliRunner).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? strings[MessageId.Unknown]);
            return 0;
        }

        if (args.Length == 0 || args[0] is not ("shutdown" or "restart"))
            return Invalid(error, strings);

        bool force = false, hybrid = false, json = false, verbose = false, elevate = false, elevatedChild = false;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string argument in args[1..])
        {
            if (!seen.Add(argument))
                return Invalid(error, strings);

            switch (argument)
            {
                case "--force": force = true; break;
                case "--hybrid": hybrid = true; break;
                case "--json": json = true; break;
                case "--verbose": verbose = true; break;
                case "--elevate": elevate = true; break;
                case "--elevated-child": elevatedChild = true; break;
                default: return Invalid(error, strings);
            }
        }

        if ((hybrid && args[0] != "shutdown") || (elevate && elevatedChild))
            return Invalid(error, strings);

        var request = new PowerRequest(args[0] == "shutdown" ? PowerAction.Shutdown : PowerAction.Restart, force, hybrid);
        Action<PowerDiagnostic>? diagnostic = verbose && !elevatedChild
            ? entry => WriteDiagnostic(entry, request, strings, error)
            : null;
        PowerResult result = controller.Execute(request, diagnostic);

        if (elevatedChild)
            return ElevatedExitCode.Encode(result);

        if (elevate && result.Status == PowerRequestStatus.AccessDenied)
        {
            launcher ??= new ElevatedProcessLauncher();
            var childArguments = new List<string> { args[0] };
            if (force) childArguments.Add("--force");
            if (hybrid) childArguments.Add("--hybrid");
            childArguments.Add("--elevated-child");

            ElevationLaunchResult launch = launcher.Launch(childArguments, waitForExit: true, hideWindow: true);
            if (launch.Status == ElevationLaunchStatus.Cancelled)
                return WriteElevationError("elevationCancelled", MessageId.ElevationCancelled, 10, 1223, request, json, output, error, strings);
            if (launch.Status == ElevationLaunchStatus.Failed)
                return WriteElevationError("elevationFailed", MessageId.ElevationLaunchFailed, 30, launch.NativeErrorCode, request, json, output, error, strings);

            if (verbose)
                error.WriteLine(strings.Format(MessageId.DiagnosticElevatedExit, launch.ExitCode?.ToString(CultureInfo.InvariantCulture) ?? strings[MessageId.Unknown]));

            if (!ElevatedExitCode.TryDecode(request, launch.ExitCode, out result))
                return WriteElevationError("elevationFailed", MessageId.ElevationUnexpectedExit, 30, launch.ExitCode, request, json, output, error, strings);
        }

        WriteResult(result, json, output, error, strings);
        return ExitCode(result.Status);
    }

    private static void WriteDiagnostic(PowerDiagnostic entry, PowerRequest request, UiStrings strings, TextWriter error)
    {
        string message = entry.Stage switch
        {
            PowerDiagnosticStage.Request => strings.Format(
                MessageId.DiagnosticRequest,
                strings[request.Action == PowerAction.Shutdown ? MessageId.GuiShutdown : MessageId.GuiRestart],
                entry.NativeFlags.GetValueOrDefault()),
            PowerDiagnosticStage.PrivilegeActivation => strings.Format(MessageId.DiagnosticPrivilege, entry.NativeCode.GetValueOrDefault()),
            PowerDiagnosticStage.ShutdownRequest => strings.Format(MessageId.DiagnosticShutdown, entry.NativeCode.GetValueOrDefault()),
            _ => throw new ArgumentOutOfRangeException(nameof(entry))
        };
        error.WriteLine(message);
    }

    private static void WriteResult(PowerResult result, bool json, TextWriter output, TextWriter error, UiStrings strings)
    {
        if (json)
        {
            string status = result.Status switch
            {
                PowerRequestStatus.Accepted => "accepted",
                PowerRequestStatus.AccessDenied => "accessDenied",
                PowerRequestStatus.OtherUsersLoggedOn => "otherUsersLoggedOn",
                PowerRequestStatus.ShutdownAlreadyInProgress => "shutdownAlreadyInProgress",
                PowerRequestStatus.ShutdownAlreadyScheduled => "shutdownAlreadyScheduled",
                PowerRequestStatus.SystemNotReady => "systemNotReady",
                _ => "failed"
            };
            WriteJson(result.Request, status, result.NativeErrorCode, output);
            return;
        }

        if (result.Status == PowerRequestStatus.Accepted)
        {
            output.WriteLine(strings[result.Request.Action == PowerAction.Shutdown ? MessageId.ShutdownAccepted : MessageId.RestartAccepted]);
            output.WriteLine(strings[MessageId.InstallUpdatesNotRequested]);
            output.WriteLine(strings[MessageId.UpdateMayStillComplete]);
            return;
        }

        error.WriteLine(strings[result.Request.Action == PowerAction.Shutdown ? MessageId.ShutdownNotRequested : MessageId.RestartNotRequested]);
        error.WriteLine(result.Status switch
        {
            PowerRequestStatus.AccessDenied => strings[MessageId.AccessDenied],
            PowerRequestStatus.OtherUsersLoggedOn => strings[MessageId.OtherUsersLoggedOn],
            PowerRequestStatus.ShutdownAlreadyInProgress => strings[MessageId.ShutdownInProgress],
            PowerRequestStatus.ShutdownAlreadyScheduled => strings[MessageId.ShutdownScheduled],
            PowerRequestStatus.SystemNotReady => strings[MessageId.SystemNotReady],
            _ => strings.Format(MessageId.WindowsError, result.NativeErrorCode?.ToString(CultureInfo.InvariantCulture) ?? strings[MessageId.Unknown])
        });
    }

    private static int WriteElevationError(
        string status,
        MessageId message,
        int exitCode,
        int? nativeErrorCode,
        PowerRequest request,
        bool json,
        TextWriter output,
        TextWriter error,
        UiStrings strings)
    {
        if (json)
            WriteJson(request, status, nativeErrorCode, output);
        else
            error.WriteLine(message == MessageId.ElevationCancelled
                ? strings[message]
                : strings.Format(message, nativeErrorCode?.ToString(CultureInfo.InvariantCulture) ?? strings[MessageId.Unknown]));
        return exitCode;
    }

    private static void WriteJson(PowerRequest request, string status, int? nativeErrorCode, TextWriter output)
    {
        string action = request.Action == PowerAction.Shutdown ? "shutdown" : "restart";
        string code = nativeErrorCode?.ToString(CultureInfo.InvariantCulture) ?? "null";
        output.WriteLine($"{{\"action\":\"{action}\",\"status\":\"{status}\",\"installUpdatesRequested\":false,\"nativeErrorCode\":{code}}}");
    }

    private static int ExitCode(PowerRequestStatus status) => status switch
    {
        PowerRequestStatus.Accepted => 0,
        PowerRequestStatus.AccessDenied => 10,
        PowerRequestStatus.OtherUsersLoggedOn => 11,
        PowerRequestStatus.ShutdownAlreadyInProgress => 12,
        PowerRequestStatus.ShutdownAlreadyScheduled => 13,
        PowerRequestStatus.SystemNotReady => 14,
        _ => 30
    };

    private static int Invalid(TextWriter error, UiStrings strings)
    {
        error.WriteLine(strings[MessageId.InvalidCommand]);
        error.WriteLine(strings[MessageId.Usage]);
        return 2;
    }
}
