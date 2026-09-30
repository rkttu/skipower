using Skipower.Windows;

namespace Skipower.Cli;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args) =>
        CliRunner.Run(args, new WindowsPowerController(), Console.Out, Console.Error);
}
