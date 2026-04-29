using System.Management;
using PPBM.Models;

namespace PPBM.Services;

public class MonitorService
{
    public static List<MonitorInfo> GetMonitors()
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

                    byte refreshByte = 0;
                    try { refreshByte = (byte)obj["CurrentRefreshRate"]; } catch { }

                    var name = instanceName;
                    var connectionType = "Unknown";
                    var refresh = (int)refreshByte;

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
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                monitors.Add(new MonitorInfo(
                    screen.DeviceName,
                    screen.Primary && System.Windows.Forms.Screen.AllScreens.Length == 1,
                    "Unknown",
                    60,
                    false
                ));
            }
        }

        return monitors;
    }
}
