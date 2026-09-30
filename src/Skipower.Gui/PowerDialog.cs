using System.Globalization;
using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Skipower.Core;
using Skipower.Windows;

namespace Skipower.Gui;

internal static class PowerDialog
{
    internal static Window Create(
        IPowerController controller,
        IElevatedProcessLauncher launcher,
        UiStrings strings,
        PowerResult? initialResult = null)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(strings);

        PowerAction? deniedAction = null;
        bool isElevatedRetry = initialResult is not null;
        var status = new Label().Text("").TextWrapping(TextWrapping.Wrap);
        var window = new Window();
        var cancel = new Button().Content(strings[MessageId.GuiCancel]).OnClick(window.Close);
        var elevate = new Button()
            .Content(strings[MessageId.GuiElevateRetry])
            .IsVisible(false);
        elevate.OnClick(RetryElevated);

        window.Title("Skipower")
            .Resizable(500, 400)
            .Padding(20)
            .Content(new StackPanel()
                .Spacing(12)
                .Children(
                    new Label().Text(strings[MessageId.GuiLead]).FontSize(16).Bold().TextWrapping(TextWrapping.Wrap),
                    new Label().Text(strings[MessageId.GuiScope]).TextWrapping(TextWrapping.Wrap),
                    new Button().Content(strings[MessageId.GuiShutdown]).OnClick(() => Execute(PowerAction.Shutdown)),
                    new Label().Text(strings[MessageId.GuiShutdownDescription]).TextWrapping(TextWrapping.Wrap),
                    new Button().Content(strings[MessageId.GuiRestart]).OnClick(() => Execute(PowerAction.Restart)),
                    new Label().Text(strings[MessageId.GuiRestartDescription]).TextWrapping(TextWrapping.Wrap),
                    status,
                    elevate,
                    cancel));

        if (initialResult is not null)
            ShowResult(initialResult);
        return window;

        void Execute(PowerAction action) => ShowResult(controller.Execute(new PowerRequest(action)));

        void ShowResult(PowerResult result)
        {
            deniedAction = result.Status == PowerRequestStatus.AccessDenied && !isElevatedRetry
                ? result.Request.Action : null;
            elevate.IsVisible(deniedAction.HasValue);
            status.Text(result.Status switch
            {
                PowerRequestStatus.Accepted => strings[MessageId.GuiAccepted],
                PowerRequestStatus.AccessDenied => strings[isElevatedRetry ? MessageId.GuiAccessDeniedElevated : MessageId.GuiAccessDenied],
                PowerRequestStatus.OtherUsersLoggedOn => strings[MessageId.GuiOtherUsers],
                PowerRequestStatus.ShutdownAlreadyInProgress => strings[MessageId.GuiInProgress],
                PowerRequestStatus.ShutdownAlreadyScheduled => strings[MessageId.GuiScheduled],
                PowerRequestStatus.SystemNotReady => strings[MessageId.GuiNotReady],
                _ => strings.Format(MessageId.GuiFailed,
                    result.NativeErrorCode?.ToString(CultureInfo.InvariantCulture) ?? strings[MessageId.Unknown])
            });
            if (result.Status == PowerRequestStatus.Accepted)
                cancel.Content(strings[MessageId.GuiClose]);
        }

        void RetryElevated()
        {
            if (deniedAction is not PowerAction action)
                return;

            elevate.IsVisible(false);
            string actionArgument = action == PowerAction.Shutdown ? "shutdown" : "restart";
            ElevationLaunchResult launch = launcher.Launch(
                ["--retry", actionArgument, "--ui-language", strings.LanguageCode],
                waitForExit: false,
                hideWindow: false);
            if (launch.Status == ElevationLaunchStatus.Started)
            {
                window.Close();
                return;
            }

            status.Text(launch.Status == ElevationLaunchStatus.Cancelled
                ? strings[MessageId.ElevationCancelled]
                : strings.Format(MessageId.ElevationLaunchFailed,
                    launch.NativeErrorCode?.ToString(CultureInfo.InvariantCulture) ?? strings[MessageId.Unknown]));
            elevate.IsVisible(true);
        }
    }
}
