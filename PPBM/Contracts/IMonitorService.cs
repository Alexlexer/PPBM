using PPBM.Models;

namespace PPBM.Contracts;

/// <summary>
/// Defines the contract for enumerating connected display monitors and their properties.
/// </summary>
public interface IMonitorService
{
    /// <summary>
    /// Retrieves a list of all connected monitors with their display parameters.
    /// Queries WMI <c>WmiMonitorBasicDisplayParams</c> first; falls back to
    /// <c>EnumDisplaySettings</c> via Win32 API if WMI returns no results.
    /// </summary>
    /// <returns>A list of <see cref="MonitorInfo"/> records for each detected monitor.</returns>
    List<MonitorInfo> GetMonitors();
}
