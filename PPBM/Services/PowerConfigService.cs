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
                var psi = new ProcessStartInfo("powercfg", $"/query SCHEME_CURRENT SUB_PROCESSOR {BoostModeGuid}")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi)!;
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (output.Contains("0x00000004"))
                    return BoostMode.EfficientAggressive;
                if (output.Contains("0x00000003"))
                    return BoostMode.EfficientEnabled;
                if (output.Contains("0x00000002"))
                    return BoostMode.Aggressive;
                if (output.Contains("0x00000001"))
                    return BoostMode.Enabled;
                return BoostMode.Disabled;
            }
            catch
            {
                return BoostMode.Aggressive;
            }
        });
    }

    public static async Task<bool> SetBoostModeAsync(BoostMode mode)
    {
        return await Task.Run(() =>
        {
            try
            {
                var value = ((int)mode).ToString("X");
                var psi = new ProcessStartInfo("powercfg")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                psi.ArgumentList.Add("/setacvalueindex");
                psi.ArgumentList.Add("SCHEME_CURRENT");
                psi.ArgumentList.Add("SUB_PROCESSOR");
                psi.ArgumentList.Add(BoostModeGuid);
                psi.ArgumentList.Add(value);

                using var proc1 = Process.Start(psi)!;
                proc1.WaitForExit();
                if (proc1.ExitCode != 0) return false;

                psi.ArgumentList.Clear();
                psi.ArgumentList.Add("/setdcvalueindex");
                psi.ArgumentList.Add("SCHEME_CURRENT");
                psi.ArgumentList.Add("SUB_PROCESSOR");
                psi.ArgumentList.Add(BoostModeGuid);
                psi.ArgumentList.Add(value);

                using var proc2 = Process.Start(psi)!;
                proc2.WaitForExit();
                if (proc2.ExitCode != 0) return false;

                psi.ArgumentList.Clear();
                psi.ArgumentList.Add("/setactive");
                psi.ArgumentList.Add("SCHEME_CURRENT");

                using var proc3 = Process.Start(psi)!;
                proc3.WaitForExit();
                return proc3.ExitCode == 0;
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
                var psi = new ProcessStartInfo("powercfg")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                psi.ArgumentList.Add("/setacvalueindex");
                psi.ArgumentList.Add("SCHEME_CURRENT");
                psi.ArgumentList.Add("SUB_PROCESSOR");
                psi.ArgumentList.Add(MaxFreqGuid);
                psi.ArgumentList.Add(percent.ToString());

                using var proc1 = Process.Start(psi)!;
                proc1.WaitForExit();

                psi.ArgumentList.Clear();
                psi.ArgumentList.Add("/setdcvalueindex");
                psi.ArgumentList.Add("SCHEME_CURRENT");
                psi.ArgumentList.Add("SUB_PROCESSOR");
                psi.ArgumentList.Add(MaxFreqGuid);
                psi.ArgumentList.Add(percent.ToString());

                using var proc2 = Process.Start(psi)!;
                proc2.WaitForExit();

                psi.ArgumentList.Clear();
                psi.ArgumentList.Add("/setactive");
                psi.ArgumentList.Add("SCHEME_CURRENT");

                using var proc3 = Process.Start(psi)!;
                proc3.WaitForExit();
                return proc3.ExitCode == 0;
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
                var psi = new ProcessStartInfo("powercfg",
                    $"-attributes SUB_PROCESSOR {BoostModeGuid} -ATTRIB_HIDE")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi)!;
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        });
    }
}
