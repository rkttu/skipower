namespace Skipower.Core;

public sealed record PowerRequest(PowerAction Action, bool Force = false, bool Hybrid = false);
