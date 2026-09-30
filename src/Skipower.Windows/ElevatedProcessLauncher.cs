using System.ComponentModel;
using System.Diagnostics;
using System.Security;

namespace Skipower.Windows;

public enum ElevationLaunchStatus
{
    Started,
    Cancelled,
    Failed
}

public sealed record ElevationLaunchResult(
    ElevationLaunchStatus Status,
    int? ExitCode = null,
    int? NativeErrorCode = null);

public interface IElevatedProcessLauncher
{
    ElevationLaunchResult Launch(IReadOnlyList<string> arguments, bool waitForExit, bool hideWindow);
}

public sealed class ElevatedProcessLauncher : IElevatedProcessLauncher
{
    public ElevationLaunchResult Launch(IReadOnlyList<string> arguments, bool waitForExit, bool hideWindow)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        string? path = Environment.ProcessPath;
        if (string.IsNullOrEmpty(path))
            return new ElevationLaunchResult(ElevationLaunchStatus.Failed);

        var startInfo = new ProcessStartInfo(path)
        {
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = AppContext.BaseDirectory,
            WindowStyle = hideWindow ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
        };
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        try
        {
            using Process? process = Process.Start(startInfo);
            if (process is null)
                return new ElevationLaunchResult(ElevationLaunchStatus.Failed);

            if (!waitForExit)
                return new ElevationLaunchResult(ElevationLaunchStatus.Started);

            process.WaitForExit();
            return new ElevationLaunchResult(ElevationLaunchStatus.Started, process.ExitCode);
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 1223)
        {
            return new ElevationLaunchResult(ElevationLaunchStatus.Cancelled, NativeErrorCode: 1223);
        }
        catch (Win32Exception exception)
        {
            return new ElevationLaunchResult(ElevationLaunchStatus.Failed, NativeErrorCode: exception.NativeErrorCode);
        }
        catch (InvalidOperationException)
        {
            return new ElevationLaunchResult(ElevationLaunchStatus.Failed);
        }
        catch (SecurityException)
        {
            return new ElevationLaunchResult(ElevationLaunchStatus.Failed);
        }
    }
}
