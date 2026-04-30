namespace PPBM.Models;

public record MonitorInfo(
    string Name,
    bool IsInternal,
    string ConnectionType,
    int CurrentRefreshRate,
    bool IsAbove60Hz
)
{
    public string RefreshWarning => IsAbove60Hz ? "Warning: Consider lowering to 60Hz" : "OK";
}
