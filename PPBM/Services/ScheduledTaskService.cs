using System.IO;
using Microsoft.Win32.TaskScheduler;
using PPBM.Models;
using System.Diagnostics;
using TaskService = Microsoft.Win32.TaskScheduler.TaskService;
using Task = System.Threading.Tasks.Task;
using TaskDefinition = Microsoft.Win32.TaskScheduler.TaskDefinition;
using TaskRunLevel = Microsoft.Win32.TaskScheduler.TaskRunLevel;
using TaskLogonType = Microsoft.Win32.TaskScheduler.TaskLogonType;

namespace PPBM.Services;

public class ScheduledTaskService
{
    private const string TaskName = "PPBM_ApplyBoostMode";
    private const string BatFileName = "PPBM_ApplyBoost.bat";

    public static bool IsTaskInstalled()
    {
        try
        {
            using var ts = new TaskService();
            var task = ts.FindTask(TaskName, false);
            return task is not null;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> InstallTaskAsync(BoostMode mode, int maxCpuPercent = 100)
    {
        return await Task.Run(() =>
        {
            try
            {
                var batPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PPBM",
                    BatFileName
                );

                Directory.CreateDirectory(Path.GetDirectoryName(batPath)!);

                var boostHex = ((int)mode).ToString("X");
                var batContent = $"""
@echo off
powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR be337238-0d82-4146-a960-4f3749d470c7 {boostHex}
powercfg /setdcvalueindex SCHEME_CURRENT SUB_PROCESSOR be337238-0d82-4146-a960-4f3749d470c7 {boostHex}
powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX {maxCpuPercent}
powercfg /setdcvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX {maxCpuPercent}
powercfg /setactive SCHEME_CURRENT
""";

                File.WriteAllText(batPath, batContent);

                using var ts = new TaskService();
                var td = ts.NewTask();
                td.RegistrationInfo.Description = "PPBM — Re-apply Processor Performance Boost Mode after Windows updates";
                td.Principal.RunLevel = TaskRunLevel.Highest;
                td.Principal.LogonType = TaskLogonType.InteractiveToken;

                var trigger = td.Triggers.Add(new LogonTrigger());
                trigger.Delay = TimeSpan.FromSeconds(30);

                td.Actions.Add(new ExecAction(batPath));

                ts.RootFolder.RegisterTaskDefinition(TaskName, td);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public static void UninstallTask()
    {
        try
        {
            using var ts = new TaskService();
            ts.RootFolder.DeleteTask(TaskName, false);
        }
        catch { }
    }
}
