using System.Management;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using PPBM.Contracts;
using PPBM.Infrastructure;
using PPBM.Models;

namespace PPBM.Services;

/// <summary>
/// Enumerates connected display monitors and their properties using WMI and Win32 API fallback.
/// Implements <see cref="IMonitorService"/>.
/// </summary>
public class MonitorService : IMonitorService
{
    /// <inheritdoc />
    public List<MonitorInfo> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\WMI",
                "SELECT Name, InstanceName, CurrentRefreshRate, CurrentHorizontalResolution, CurrentVerticalResolution FROM WmiMonitorBasicDisplayParams");

            foreach (ManagementObject obj in searcher.Get())
            {
                try
                {
                    var instanceName = obj["InstanceName"]?.ToString() ?? "";
                    var isInternal = instanceName.StartsWith("DISPLAY", StringComparison.OrdinalIgnoreCase) &&
                                     instanceName.Contains("_0", StringComparison.OrdinalIgnoreCase);

                    int refresh = 0;
                    try
                    {
                        var rawRefresh = obj["CurrentRefreshRate"];
                        if (rawRefresh is not null)
                            int.TryParse(rawRefresh.ToString(), out refresh);
                    }
                    catch { }

                    var name = instanceName;
                    var connectionType = "Unknown";

                    bool isAbove60 = refresh > 60;

                    monitors.Add(new MonitorInfo(
                        name,
                        isInternal,
                        connectionType,
                        refresh,
                        isAbove60
                    ));
                }
                catch { }
            }
        }
        catch { }

        if (monitors.Count == 0)
        {
            foreach (var screen in Screen.AllScreens)
            {
                var hz = GetRefreshRateFromDevice(screen.DeviceName) ?? 60;
                monitors.Add(new MonitorInfo(
                    screen.DeviceName,
                    screen.Primary && Screen.AllScreens.Length == 1,
                    "Unknown",
                    hz,
                    hz > 60
                ));
            }
        }

        return monitors;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref Devmode lpDevMode);

    private const int ENUM_CURRENT_SETTINGS = -1;

    private static int? GetRefreshRateFromDevice(string deviceName)
    {
        try
        {
            var dm = new Devmode
            {
                dmDeviceName = new string('\0', 32),
                dmFormName = new string('\0', 32),
                dmSize = (short)Marshal.SizeOf<Devmode>()
            };
            if (EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref dm))
            {
                if (dm.dmDisplayFrequency > 0)
                    return dm.dmDisplayFrequency;
            }
        }
        catch { }
        return null;
    }
}
