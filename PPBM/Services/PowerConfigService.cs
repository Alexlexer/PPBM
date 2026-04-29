using System.Diagnostics;
using System.IO;
using PPBM.Models;

namespace PPBM.Services;

public static class PowerConfigService
{
    private const string BoostGuid = "be337238-0d82-4146-a960-4f3749d470c7";
    private const string SubProcessor = "54533251-82be-4824-96c1-47b60b740d00";

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PPBM", "debug.log");

    public static async Task<BoostMode> GetCurrentBoostModeAsync()
    {
        var raw = await RunPowerCfgAsync(
            $"/query SCHEME_CURRENT {SubProcessor} {BoostGuid}");

        Log($"--- /query raw output ---\n{raw ?? "null"}");

        if (raw is null)
        {
            Log("  -> /query returned null, trying /getacvalueindex");
            raw = await RunPowerCfgAsync(
                $"/getacvalueindex SCHEME_CURRENT {SubProcessor} {BoostGuid}");
            Log($"--- /getacvalueindex raw output ---\n{raw ?? "null"}");
        }

        var result = ParseBoostMode(raw);
        Log($"  -> parsed to {(int)result} ({result})");
        return result;
    }

    private static BoostMode ParseBoostMode(string? raw)
    {
        if (raw is null) return BoostMode.Aggressive;

        // Find all 0x... hex values, pick the LAST one
        int last = 2;
        int i = 0;
        while ((i = raw.IndexOf("0x", i, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var end = i + 2;
            while (end < raw.Length && IsHexDigit(raw[end])) end++;
            var hex = raw.Substring(i, end - i);
            var num = int.TryParse(raw.AsSpan(i + 2, end - i - 2),
                System.Globalization.NumberStyles.HexNumber, null, out var v) ? v : -1;
            Log($"  Found hex: {hex} -> int={num}");
            if (num >= 0 && num <= 4) last = num;
            i = end;
        }

        return last switch
        {
            0 => BoostMode.Disabled,
            1 => BoostMode.Enabled,
            2 => BoostMode.Aggressive,
            3 => BoostMode.EfficientEnabled,
            4 => BoostMode.EfficientAggressive,
            _ => BoostMode.Aggressive
        };
    }

    private static bool IsHexDigit(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    public static async Task<bool> SetBoostModeAsync(BoostMode mode)
    {
        var val = ((int)mode).ToString();
        Log($"Setting boost mode to {val} ({mode})");

        var ok1 = await RunPowerCfgAsync(
            $"/setacvalueindex SCHEME_CURRENT {SubProcessor} {BoostGuid} {val}");
        var ok2 = await RunPowerCfgAsync(
            $"/setdcvalueindex SCHEME_CURRENT {SubProcessor} {BoostGuid} {val}");
        var ok3 = await RunPowerCfgAsync(
            $"/setactive SCHEME_CURRENT");

        Log($"  AC={ok1 is not null} DC={ok2 is not null} ACTIVE={ok3 is not null}");
        return ok1 is not null && ok2 is not null && ok3 is not null;
    }

    public static async Task<bool> SetMaxCpuFrequencyAsync(int percent)
    {
        var p = percent.ToString();
        var ok1 = await RunPowerCfgAsync(
            $"/setacvalueindex SCHEME_CURRENT {SubProcessor} PROCTHROTTLEMAX {p}");
        var ok2 = await RunPowerCfgAsync(
            $"/setdcvalueindex SCHEME_CURRENT {SubProcessor} PROCTHROTTLEMAX {p}");
        var ok3 = await RunPowerCfgAsync("/setactive SCHEME_CURRENT");
        return ok1 is not null && ok2 is not null && ok3 is not null;
    }

    public static async Task<bool> UnhideBoostSettingAsync()
    {
        return await RunPowerCfgAsync(
            $"-attributes SUB_PROCESSOR {BoostGuid} -ATTRIB_HIDE") is not null;
    }

    private static async Task<string?> RunPowerCfgAsync(string args)
    {
        return await Task.Run(() =>
        {
            try
            {
                Log($"> powercfg {args}");
                var psi = new ProcessStartInfo("powercfg", args)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                using var proc = Process.Start(psi)!;
                var stdout = proc.StandardOutput.ReadToEnd();
                var stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit(10000);
                Log($"  exit={proc.ExitCode} stdout=[{stdout?.Trim()}] stderr=[{stderr?.Trim()}]");
                return proc.ExitCode == 0 ? stdout.Trim() : null;
            }
            catch (Exception ex)
            {
                Log($"  EXCEPTION: {ex.Message}");
                return null;
            }
        });
    }

    private static void Log(string msg)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath,
                $"[{DateTime.Now:HH:mm:ss.fff}] {msg}{Environment.NewLine}");
        }
        catch { }
    }
}
