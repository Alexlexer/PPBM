using System.Diagnostics;
using System.IO;
using Microsoft.Win32.TaskScheduler;
using PPBM.Contracts;
using PPBM.Models;
using TaskService = Microsoft.Win32.TaskScheduler.TaskService;
using TaskDefinition = Microsoft.Win32.TaskScheduler.TaskDefinition;
using TaskRunLevel = Microsoft.Win32.TaskScheduler.TaskRunLevel;
using TaskLogonType = Microsoft.Win32.TaskScheduler.TaskLogonType;
using SysTask = System.Threading.Tasks.Task;

namespace PPBM.Services;

/// <summary>
/// Manages the Windows scheduled task that re-applies Processor Performance Boost Mode
/// after system updates and user logon. Implements <see cref="IScheduledTaskService"/>.
/// </summary>
public class ScheduledTaskService : IScheduledTaskService
{
    private const string TaskName = "PPBM_ApplyBoostMode";
    private const string BatFileName = "PPBM_ApplyBoost.bat";

    /// <inheritdoc />
    public bool IsTaskInstalled()
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

    /// <inheritdoc />
    public async Task<bool> InstallTaskAsync(BoostMode mode, int maxCpuPercent = 100, CancellationToken ct = default)
    {
        return await SysTask.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return false;
            }
        }, ct);
    }

    /// <inheritdoc />
    public void UninstallTask()
    {
        try
        {
            using var ts = new TaskService();
            ts.RootFolder.DeleteTask(TaskName, false);
        }
        catch
        {
            // Task may not exist or insufficient permissions; swallow.
        }
    }
}
