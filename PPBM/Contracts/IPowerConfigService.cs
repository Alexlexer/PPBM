using PPBM.Models;

namespace PPBM.Contracts;

/// <summary>
/// Defines the contract for interacting with Windows power configuration via <c>powercfg.exe</c>.
/// Covers querying and modifying the Processor Performance Boost Mode and Max CPU Frequency settings.
/// </summary>
public interface IPowerConfigService
{
    /// <summary>
    /// Queries the current Processor Performance Boost Mode from the active power scheme.
    /// Attempts to unhide the setting if it is not visible.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current <see cref="BoostMode"/> value.</returns>
    Task<BoostMode> GetCurrentBoostModeAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the Processor Performance Boost Mode on both AC and DC power indices.
    /// </summary>
    /// <param name="mode">The target boost mode.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if all powercfg commands succeeded; otherwise <c>false</c>.</returns>
    Task<bool> SetBoostModeAsync(BoostMode mode, CancellationToken ct = default);

    /// <summary>
    /// Sets the maximum CPU frequency as a percentage on both AC and DC power indices.
    /// </summary>
    /// <param name="percent">Frequency limit in percent (0-100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if all powercfg commands succeeded; otherwise <c>false</c>.</returns>
    Task<bool> SetMaxCpuFrequencyAsync(int percent, CancellationToken ct = default);

    /// <summary>
    /// Removes the hidden attribute from the Processor Performance Boost Mode power setting,
    /// making it visible in Control Panel > Power Options > Advanced Settings.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the command succeeded; otherwise <c>false</c>.</returns>
    Task<bool> UnhideBoostSettingAsync(CancellationToken ct = default);
}
