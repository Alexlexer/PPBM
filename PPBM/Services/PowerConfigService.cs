using System.Diagnostics;
using PPBM.Models;

namespace PPBM.Services;

public class PowerConfigService
{
    private const string BoostModeGuid = "be337238-0d82-4146-a960-4f3749d470c7";
    private const string MaxFreqGuid = "PROCTHROTTLEMAX";

    public static async Task<BoostMode> GetCurrentBoostModeAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var hex = RunPowerCfgAndGetOutput(
                    "/getacvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", BoostModeGuid);

                hex = hex?.Trim();
                return !string.IsNullOrEmpty(hex)
                    ? ParseBoostMode(hex)
                    : BoostMode.Aggressive;
            }
            catch
            {
                return BoostMode.Aggressive;
            }
        });
    }

    private static string? RunPowerCfgAndGetOutput(params string[] args)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("powercfg")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            }
        };
        foreach (var arg in args)
            process.StartInfo.ArgumentList.Add(arg);
        process.Start();
        process.WaitForExit(10000);
        return process.ExitCode == 0
            ? process.StandardOutput.ReadToEnd().Trim()
            : null;
    }

    private static BoostMode ParseBoostMode(string hex)
    {
        // Handle both 0x2 and 0x00000002 formats
        var clean = hex.Replace("0x", "").Replace("0X", "").Trim();
        var value = int.TryParse(clean, System.Globalization.NumberStyles.HexNumber, null, out var v) ? v : 2;
        return value switch
        {
            0 => BoostMode.Disabled,
            1 => BoostMode.Enabled,
            2 => BoostMode.Aggressive,
            3 => BoostMode.EfficientEnabled,
            4 => BoostMode.EfficientAggressive,
            _ => BoostMode.Aggressive
        };
    }

    private static int RunPowerCfg(params string[] args)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("powercfg")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        foreach (var arg in args)
            process.StartInfo.ArgumentList.Add(arg);
        process.Start();
        process.WaitForExit(10000);
        return process.ExitCode;
    }

    public static async Task<bool> SetBoostModeAsync(BoostMode mode)
    {
        return await Task.Run(() =>
        {
            try
            {
                var value = ((int)mode).ToString("X");
                var ok1 = RunPowerCfg("/setacvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", BoostModeGuid, value) == 0;
                var ok2 = RunPowerCfg("/setdcvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", BoostModeGuid, value) == 0;
                var ok3 = RunPowerCfg("/setactive", "SCHEME_CURRENT") == 0;
                return ok1 && ok2 && ok3;
            }
            catch
            {
                return false;
            }
        });
    }

    public static async Task<bool> SetMaxCpuFrequencyAsync(int percent)
    {
        return await Task.Run(() =>
        {
            try
            {
                var p = percent.ToString();
                var ok1 = RunPowerCfg("/setacvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", MaxFreqGuid, p) == 0;
                var ok2 = RunPowerCfg("/setdcvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", MaxFreqGuid, p) == 0;
                var ok3 = RunPowerCfg("/setactive", "SCHEME_CURRENT") == 0;
                return ok1 && ok2 && ok3;
            }
            catch
            {
                return false;
            }
        });
    }

    public static async Task<bool> UnhideBoostSettingAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                return RunPowerCfg("-attributes", "SUB_PROCESSOR", BoostModeGuid, "-ATTRIB_HIDE") == 0;
            }
            catch
            {
                return false;
            }
        });
    }
}
