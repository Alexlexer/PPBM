using System.Text.RegularExpressions;
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
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo("powercfg", $"/query SCHEME_CURRENT SUB_PROCESSOR {BoostModeGuid}")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(5000);

                var match = Regex.Match(output, @"0x([0-9A-Fa-f]{8})", RegexOptions.Multiline);
                return match.Success
                    ? ParseBoostMode(match.Value)
                    : BoostMode.Aggressive;
            }
            catch
            {
                return BoostMode.Aggressive;
            }
        });
    }

    private static BoostMode ParseBoostMode(string hex)
    {
        return hex switch
        {
            "0x00000000" => BoostMode.Disabled,
            "0x00000001" => BoostMode.Enabled,
            "0x00000002" => BoostMode.Aggressive,
            "0x00000003" => BoostMode.EfficientEnabled,
            "0x00000004" => BoostMode.EfficientAggressive,
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
