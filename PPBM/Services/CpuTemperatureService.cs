using LibreHardwareMonitor.Hardware;

namespace PPBM.Services;

public class CpuTemperatureService : IDisposable
{
    private readonly Computer _computer;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(2);
    private DateTime _lastUpdate = DateTime.MinValue;

    public CpuTemperatureService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = false,
            IsMemoryEnabled = false,
            IsMotherboardEnabled = false,
            IsControllerEnabled = false,
            IsNetworkEnabled = false,
            IsStorageEnabled = false,
            IsPsuEnabled = false,
            IsBatteryEnabled = false
        };
        _computer.Open();
    }

    public (float? PackageTemp, float? MaxCoreTemp, string CpuName) GetCpuTemperatures()
    {
        try
        {
            var now = DateTime.UtcNow;
            if (now - _lastUpdate > _updateInterval)
            {
                foreach (var hardware in _computer.Hardware)
                    hardware.Update();
                _lastUpdate = now;
            }

            foreach (var hardware in _computer.Hardware)
            {
                if (hardware.HardwareType != HardwareType.Cpu)
                    continue;

                var cpuName = hardware.Name;
                float? packageTemp = null;
                float? maxCoreTemp = null;

                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        if (sensor.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
                            packageTemp = sensor.Value;
                        else if (sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) &&
                                 !sensor.Name.Contains("Distance", StringComparison.OrdinalIgnoreCase))
                        {
                            if (maxCoreTemp is null || sensor.Value > maxCoreTemp)
                                maxCoreTemp = sensor.Value;
                        }
                    }
                }

                return (packageTemp, maxCoreTemp, cpuName);
            }
        }
        catch { }

        return (null, null, "Unknown");
    }

    public float? GetCpuLoad()
    {
        try
        {
            foreach (var hardware in _computer.Hardware)
            {
                if (hardware.HardwareType != HardwareType.Cpu)
                    continue;

                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Load &&
                        sensor.Name.Contains("Total", StringComparison.OrdinalIgnoreCase))
                        return sensor.Value;
                }
            }
        }
        catch { }

        return null;
    }

    public void Dispose()
    {
        try { _computer.Close(); }
        catch { }
    }
}
