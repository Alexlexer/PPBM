using System.Management;
using PPBM.Models;
using System.Windows.Forms;
using System.Runtime.InteropServices;

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

                    // Try to read refresh from WMI result (may be different types depending on driver)
                    int refresh = 0;
                    try
                    {
                        var rawRefresh = obj["CurrentRefreshRate"];
                        if (rawRefresh is not null)
                        {
                            int.TryParse(rawRefresh.ToString(), out refresh);
                        }
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

        // If WMI did not return monitors or did not include refresh, enumerate screens via EnumDisplaySettings
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

    // Use EnumDisplaySettings to get the current display frequency for a device (more reliable than WMI)
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

    private const int ENUM_CURRENT_SETTINGS = -1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        // remaining fields omitted
    }

    private static int? GetRefreshRateFromDevice(string deviceName)
    {
        try
        {
            var dm = new DEVMODE();
            dm.dmDeviceName = new string('\0', 32);
            dm.dmFormName = new string('\0', 32);
            dm.dmSize = (short)Marshal.SizeOf<DEVMODE>();
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
