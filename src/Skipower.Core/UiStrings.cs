using System.Globalization;

namespace Skipower.Core;

public enum MessageId
{
    Usage,
    InvalidCommand,
    ShutdownAccepted,
    RestartAccepted,
    InstallUpdatesNotRequested,
    UpdateMayStillComplete,
    ShutdownNotRequested,
    RestartNotRequested,
    AccessDenied,
    OtherUsersLoggedOn,
    ShutdownInProgress,
    ShutdownScheduled,
    SystemNotReady,
    WindowsError,
    Unknown,
    ElevationCancelled,
    ElevationLaunchFailed,
    ElevationUnexpectedExit,
    DiagnosticRequest,
    DiagnosticPrivilege,
    DiagnosticShutdown,
    DiagnosticElevatedExit,
    GuiLead,
    GuiScope,
    GuiShutdown,
    GuiShutdownDescription,
    GuiRestart,
    GuiRestartDescription,
    GuiCancel,
    GuiClose,
    GuiElevateRetry,
    GuiAccepted,
    GuiAccessDenied,
    GuiAccessDeniedElevated,
    GuiOtherUsers,
    GuiInProgress,
    GuiScheduled,
    GuiNotReady,
    GuiFailed
}

public sealed class UiStrings
{
    private UiStrings(string languageCode) => LanguageCode = languageCode;

    public string LanguageCode { get; }

    public static UiStrings ForCurrentCulture() => ForCulture(CultureInfo.CurrentUICulture);

    public static UiStrings ForCulture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return ForLanguage(culture.TwoLetterISOLanguageName);
    }

    public static UiStrings ForLanguage(string? languageCode) =>
        new(string.Equals(languageCode, "ko", StringComparison.OrdinalIgnoreCase) ? "ko" : "en");

    public string this[MessageId id] => LanguageCode == "ko" ? KoreanStrings.Get(id) : EnglishStrings.Get(id);

    public string Format(MessageId id, params object?[] values) =>
        string.Format(CultureInfo.CurrentCulture, this[id], values);
}
