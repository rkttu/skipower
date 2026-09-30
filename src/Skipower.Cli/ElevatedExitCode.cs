using Skipower.Core;

namespace Skipower.Cli;

internal static class ElevatedExitCode
{
    private const int Marker = 0x5A000000;
    private const int Mask = unchecked((int)0xFF000000);
    private const int NoNativeCode = 0xFFFF;

    internal static int Encode(PowerResult result)
    {
        int code = result.NativeErrorCode is >= 0 and < NoNativeCode
            ? result.NativeErrorCode.Value : NoNativeCode;
        return Marker | ((int)result.Status << 16) | code;
    }

    internal static bool TryDecode(PowerRequest request, int? exitCode, out PowerResult result)
    {
        if (exitCode is not int value || (value & Mask) != Marker)
        {
            result = new PowerResult(request, PowerRequestStatus.Failed);
            return false;
        }

        int statusValue = (value >> 16) & 0xFF;
        if (!Enum.IsDefined((PowerRequestStatus)statusValue))
        {
            result = new PowerResult(request, PowerRequestStatus.Failed);
            return false;
        }

        int nativeCode = value & 0xFFFF;
        result = new PowerResult(
            request,
            (PowerRequestStatus)statusValue,
            nativeCode == NoNativeCode ? null : nativeCode);
        return true;
    }
}
