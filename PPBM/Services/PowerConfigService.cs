using System.Diagnostics;
using PPBM.Models;

namespace PPBM.Services;

public class PowerConfigService
{
    private const string BoostAlias = "PERFBOOSTMODE";
    private const string BoostGuid = "be337238-0d82-4146-a960-4f3749d470c7";
    private const string MaxFreqGuid = "PROCTHROTTLEMAX";

    public static async Task<BoostMode> GetCurrentBoostModeAsync()
    {
        var output = await RunPowerCfgAsync("/query", "SCHEME_CURRENT", "SUB_PROCESSOR", BoostGuid);
        if (output is null) return BoostMode.Aggressive;

        var value = ParseLastHexValue(output);
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

    private static int ParseLastHexValue(string text)
    {
        // powercfg /query outputs:
        //   Possible Setting Index: 0x00000000   ← these come first
        //   Possible Setting Index: 0x00000001
        //   Current AC Power Setting Index: 0x02 ← this is last
        // We pick the LAST hex value — language-independent
        var last = 2;
        var idx = 0;
        while ((idx = text.IndexOf("0x", idx, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var end = idx + 2;
            while (end < text.Length && IsHexChar(text[end])) end++;
            var hex = text.AsSpan(idx + 2, end - idx - 2);
            if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var v))
                last = v;
            idx = end;
        }
        return last;
    }

    private static bool IsHexChar(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    public static async Task<bool> SetBoostModeAsync(BoostMode mode)
    {
        var val = ((int)mode).ToString();
        var ok1 = await RunPowerCfgAsync("/setacvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", BoostAlias, val) is not null;
        var ok2 = await RunPowerCfgAsync("/setdcvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", BoostAlias, val) is not null;
        var ok3 = await RunPowerCfgAsync("/setactive", "SCHEME_CURRENT") is not null;
        return ok1 && ok2 && ok3;
    }

    public static async Task<bool> SetMaxCpuFrequencyAsync(int percent)
    {
        var p = percent.ToString();
        var ok1 = await RunPowerCfgAsync("/setacvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", MaxFreqGuid, p) is not null;
        var ok2 = await RunPowerCfgAsync("/setdcvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", MaxFreqGuid, p) is not null;
        var ok3 = await RunPowerCfgAsync("/setactive", "SCHEME_CURRENT") is not null;
        return ok1 && ok2 && ok3;
    }

    public static async Task<bool> UnhideBoostSettingAsync()
    {
        return await RunPowerCfgAsync("-attributes", "SUB_PROCESSOR", BoostGuid, "-ATTRIB_HIDE") is not null;
    }

    private static async Task<string?> RunPowerCfgAsync(params string[] args)
    {
        return await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo("powercfg")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                foreach (var arg in args)
                    psi.ArgumentList.Add(arg);

                using var process = new Process { StartInfo = psi };
                process.Start();

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit(10000);

                return process.ExitCode == 0 ? output.Trim() : null;
            }
            catch
            {
                return null;
            }
        });
    }
}
