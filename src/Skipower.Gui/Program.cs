using Aprillz.MewUI;
using Skipower.Core;
using Skipower.Gui;
using Skipower.Windows;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var controller = new WindowsPowerController();
        var strings = UiStrings.ForCurrentCulture();
        PowerResult? initialResult = null;
        if (args.Length == 4 && args[0] == "--retry" && args[2] == "--ui-language" &&
            args[1] is "shutdown" or "restart" && args[3] is "en" or "ko")
        {
            strings = UiStrings.ForLanguage(args[3]);
            var action = args[1] == "shutdown" ? PowerAction.Shutdown : PowerAction.Restart;
            initialResult = controller.Execute(new PowerRequest(action));
        }

        Win32Platform.Register();
        GdiBackend.Register();
        Application.Run(PowerDialog.Create(controller, new ElevatedProcessLauncher(), strings, initialResult));
    }
}
