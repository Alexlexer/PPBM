using System.Diagnostics;
using System.IO;
using PPBM.Models;

namespace PPBM.Services;

public static class PowerConfigService
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PPBM", "debug.log");

    private const string BoostSettingGuid = "be337238-0d82-4146-a960-4f3749d470c7";

    public static async Task<BoostMode> GetCurrentBoostModeAsync()
    {
        var mode = await TryGetBoostModeAsync();
        if (mode is not null)
        {
            Log($"  -> {(int)mode.Value}");
            return mode.Value;
        }

        Log("Boost setting hidden, attempting to unhide...");
        await ExecBoolAsync("-attributes", "SUB_PROCESSOR", BoostSettingGuid, "-ATTRIB_HIDE");

        mode = await TryGetBoostModeAsync();
        if (mode is not null)
        {
            Log($"  -> {(int)mode.Value}");
            return mode.Value;
        }

        Log("  -> setting still not found, defaulting to Aggressive");
        return BoostMode.Aggressive;
    }

    private static async Task<BoostMode?> TryGetBoostModeAsync()
    {
        var raw = await ExecAsync("/query", "SCHEME_CURRENT", "SUB_PROCESSOR", BoostSettingGuid);
        Log($"--- /query output ---\n{raw ?? "null"}");

        if (raw is null) return null;

        int? found = null;
        int i = 0;
        while ((i = raw.IndexOf("0x", i)) >= 0)
        {
            var end = i + 2;
            while (end < raw.Length && IsHex(raw[end])) end++;
            if (int.TryParse(raw.AsSpan(i + 2, end - i - 2),
                    System.Globalization.NumberStyles.HexNumber, null, out var v) && v <= 4)
                found = v;
            i = end;
        }

        if (found is null) return null;

        return found switch
        {
            0 => BoostMode.Disabled,
            1 => BoostMode.Enabled,
            2 => BoostMode.Aggressive,
            3 => BoostMode.EfficientEnabled,
            4 => BoostMode.EfficientAggressive,
            _ => null
        };
    }

    private static bool IsHex(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    public static async Task<bool> SetBoostModeAsync(BoostMode mode)
    {
        var val = ((int)mode).ToString();
        Log($"set boost={val} ({mode})");
        var ok1 = await ExecBoolAsync("/setacvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", "PERFBOOSTMODE", val);
        var ok2 = await ExecBoolAsync("/setdcvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", "PERFBOOSTMODE", val);
        var ok3 = await ExecBoolAsync("/setactive", "SCHEME_CURRENT");
        Log($"  AC={ok1} DC={ok2} ACT={ok3}");
        return ok1 && ok2 && ok3;
    }

    public static async Task<bool> SetMaxCpuFrequencyAsync(int percent)
    {
        var p = percent.ToString();
        var ok1 = await ExecBoolAsync("/setacvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", "PROCTHROTTLEMAX", p);
        var ok2 = await ExecBoolAsync("/setdcvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", "PROCTHROTTLEMAX", p);
        var ok3 = await ExecBoolAsync("/setactive", "SCHEME_CURRENT");
        return ok1 && ok2 && ok3;
    }

    public static async Task<bool> UnhideBoostSettingAsync()
    {
        return await ExecBoolAsync("-attributes", "SUB_PROCESSOR", BoostSettingGuid, "-ATTRIB_HIDE");
    }

    private static async Task<string?> ExecAsync(params string[] args)
    {
        return await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo("powercfg")
                {
                    RedirectStandardOutput = true, RedirectStandardError = true,
                    UseShellExecute = false, CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                foreach (var a in args) psi.ArgumentList.Add(a);

                Log($"> powercfg {string.Join(" ", args)}");
                using var proc = Process.Start(psi)!;
                var o = proc.StandardOutput.ReadToEnd();
                var e = proc.StandardError.ReadToEnd();
                proc.WaitForExit(10000);
                Log($"  exit={proc.ExitCode} o=[{o?.Trim()}] e=[{e?.Trim()}]");
                return proc.ExitCode == 0 ? o.Trim() : null;
            }
            catch (Exception ex) { Log($"  EXCEPTION: {ex.Message}"); return null; }
        });
    }

    private static async Task<bool> ExecBoolAsync(params string[] args)
    {
        return await ExecAsync(args) is not null;
    }

    private static void Log(string msg)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (dir is not null) Directory.CreateDirectory(dir);
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n");
        }
        catch { }
    }
}
