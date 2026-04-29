using LibreHardwareMonitor.Hardware;

namespace PPBM.Services;

public class CpuTemperatureService : IDisposable
{
    private readonly Computer _computer;
    private DateTime _lastUpdate = DateTime.MinValue;
    private bool _initialized;

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
        WarmUp();
    }

    private void WarmUp()
    {
        try
        {
            foreach (var hardware in _computer.Hardware)
                hardware.Update();
            _initialized = true;
        }
        catch { }
    }

    public (float? PackageTemp, float? MaxCoreTemp, string CpuName) GetCpuTemperatures()
    {
        try
        {
            var now = DateTime.UtcNow;
            if (now - _lastUpdate > TimeSpan.FromSeconds(2))
            {
                foreach (var hardware in _computer.Hardware)
                    hardware.Update();
                _lastUpdate = now;
                if (!_initialized && _computer.Hardware.Count > 0)
                    _initialized = true;
            }

            foreach (var hardware in _computer.Hardware)
            {
                if (hardware.HardwareType != HardwareType.Cpu)
                    continue;

                var cpuName = string.IsNullOrEmpty(hardware.Name) ? "Unknown CPU" : hardware.Name;
                float? packageTemp = null;
                float? maxCoreTemp = null;

                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType != SensorType.Temperature)
                        continue;

                    if (sensor.Name.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
                        sensor.Name.Contains("CPU", StringComparison.OrdinalIgnoreCase) ||
                        sensor.Name.Contains("Tctl", StringComparison.OrdinalIgnoreCase) ||
                        sensor.Name.Contains("Tdie", StringComparison.OrdinalIgnoreCase))
                    {
                        packageTemp ??= sensor.Value;
                    }

                    if (sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) &&
                        !sensor.Name.Contains("Distance", StringComparison.OrdinalIgnoreCase))
                    {
                        if (maxCoreTemp is null || sensor.Value > maxCoreTemp)
                            maxCoreTemp = sensor.Value;
                    }
                }

                if (packageTemp is null && maxCoreTemp is null)
                {
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                        {
                            packageTemp = sensor.Value;
                            break;
                        }
                    }
                }

                return (packageTemp, maxCoreTemp, cpuName);
            }
        }
        catch { }

        return (null, null, "Initializing sensors...");
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
