namespace Skipower.Core;

public interface IPowerController
{
    PowerResult Execute(PowerRequest request, Action<PowerDiagnostic>? diagnostic = null);
}
