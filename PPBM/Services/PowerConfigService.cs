using System.Diagnostics;
using System.IO;
using PPBM.Contracts;
using PPBM.Models;

namespace PPBM.Services;

/// <summary>
/// Manages Windows power configuration via <c>powercfg.exe</c> CLI.
/// Provides querying and modification of Processor Performance Boost Mode and Max CPU Frequency.
/// Implements <see cref="IPowerConfigService"/>.
/// </summary>
public class PowerConfigService : IPowerConfigService
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PPBM", "debug.log");

    private const string BoostSettingGuid = "be337238-0d82-4146-a960-4f3749d470c7";

    /// <inheritdoc />
    public async Task<BoostMode> GetCurrentBoostModeAsync(CancellationToken ct = default)
    {
        var mode = await TryGetBoostModeAsync(ct);
        if (mode is not null)
        {
            Log($"  -> {(int)mode.Value}");
            return mode.Value;
        }

        Log("Boost setting hidden, attempting to unhide...");
        await ExecBoolAsync(ct, "-attributes", "SUB_PROCESSOR", BoostSettingGuid, "-ATTRIB_HIDE");

        mode = await TryGetBoostModeAsync(ct);
        if (mode is not null)
        {
            Log($"  -> {(int)mode.Value}");
            return mode.Value;
        }

        Log("  -> setting still not found, defaulting to Aggressive");
        return BoostMode.Aggressive;
    }

    private async Task<BoostMode?> TryGetBoostModeAsync(CancellationToken ct)
    {
        var raw = await ExecAsync(ct, "/query", "SCHEME_CURRENT", "SUB_PROCESSOR", BoostSettingGuid);
        Log($"--- /query output ---\n{raw ?? "null"}");

        if (raw is null) return null;

        int? found = null;
        int i = 0;
        while ((i = raw.IndexOf("0x", i, StringComparison.Ordinal)) >= 0)
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

    /// <inheritdoc />
    public async Task<bool> SetBoostModeAsync(BoostMode mode, CancellationToken ct = default)
    {
        var val = ((int)mode).ToString();
        Log($"set boost={val} ({mode})");
        var ok1 = await ExecBoolAsync(ct, "/setacvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", "PERFBOOSTMODE", val);
        var ok2 = await ExecBoolAsync(ct, "/setdcvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", "PERFBOOSTMODE", val);
        var ok3 = await ExecBoolAsync(ct, "/setactive", "SCHEME_CURRENT");
        Log($"  AC={ok1} DC={ok2} ACT={ok3}");
        return ok1 && ok2 && ok3;
    }

    /// <inheritdoc />
    public async Task<bool> SetMaxCpuFrequencyAsync(int percent, CancellationToken ct = default)
    {
        var p = percent.ToString();
        var ok1 = await ExecBoolAsync(ct, "/setacvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", "PROCTHROTTLEMAX", p);
        var ok2 = await ExecBoolAsync(ct, "/setdcvalueindex", "SCHEME_CURRENT", "SUB_PROCESSOR", "PROCTHROTTLEMAX", p);
        var ok3 = await ExecBoolAsync(ct, "/setactive", "SCHEME_CURRENT");
        return ok1 && ok2 && ok3;
    }

    /// <inheritdoc />
    public async Task<bool> UnhideBoostSettingAsync(CancellationToken ct = default)
    {
        return await ExecBoolAsync(ct, "-attributes", "SUB_PROCESSOR", BoostSettingGuid, "-ATTRIB_HIDE");
    }

    private async Task<string?> ExecAsync(CancellationToken ct, params string[] args)
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
            foreach (var a in args) psi.ArgumentList.Add(a);

            Log($"> powercfg {string.Join(" ", args)}");
            using var proc = Process.Start(psi);
            if (proc is null)
            {
                Log("  Process.Start returned null");
                return null;
            }

            var readOut = proc.StandardOutput.ReadToEndAsync();
            var readErr = proc.StandardError.ReadToEndAsync();

            await proc.WaitForExitAsync(ct);

            var o = await readOut;
            var e = await readErr;
            Log($"  exit={proc.ExitCode} o=[{o?.Trim()}] e=[{e?.Trim()}]");
            return proc.ExitCode == 0 ? o?.Trim() : null;
        }
        catch (OperationCanceledException)
        {
            Log("  CANCELLED");
            throw;
        }
        catch (Exception ex)
        {
            Log($"  EXCEPTION: {ex.Message}");
            return null;
        }
    }

    private async Task<bool> ExecBoolAsync(CancellationToken ct, params string[] args)
    {
        return await ExecAsync(ct, args) is not null;
    }

    private static void Log(string msg)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (dir is not null) Directory.CreateDirectory(dir);
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n");
        }
        catch
        {
            // Best-effort logging; swallowing file I/O failures.
        }
    }
}
