namespace Skipower.Core;

public enum PowerDiagnosticStage
{
    Request,
    PrivilegeActivation,
    ShutdownRequest
}

public readonly record struct PowerDiagnostic(
    PowerDiagnosticStage Stage,
    uint? NativeFlags = null,
    uint? NativeCode = null);
