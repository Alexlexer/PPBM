using PPBM.Models;

namespace PPBM.Contracts;

/// <summary>
/// Defines the contract for managing the Windows scheduled task that re-applies
/// Processor Performance Boost Mode after system updates and user logon.
/// </summary>
public interface IScheduledTaskService
{
    /// <summary>
    /// Checks whether the "PPBM_ApplyBoostMode" scheduled task is currently registered.
    /// </summary>
    /// <returns><c>true</c> if the task exists; otherwise <c>false</c>.</returns>
    bool IsTaskInstalled();

    /// <summary>
    /// Creates a scheduled task that re-applies the specified boost mode and max CPU frequency
    /// on user logon (with a 30-second delay) to survive Windows feature updates.
    /// </summary>
    /// <param name="mode">The boost mode to persist.</param>
    /// <param name="maxCpuPercent">Maximum CPU frequency percentage (50-100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the task was created successfully; otherwise <c>false</c>.</returns>
    Task<bool> InstallTaskAsync(BoostMode mode, int maxCpuPercent = 100, CancellationToken ct = default);

    /// <summary>
    /// Removes the "PPBM_ApplyBoostMode" scheduled task.
    /// </summary>
    void UninstallTask();
}
