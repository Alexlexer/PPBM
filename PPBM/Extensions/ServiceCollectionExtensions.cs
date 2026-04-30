using Microsoft.Extensions.DependencyInjection;
using PPBM.Contracts;
using PPBM.Services;
using PPBM.ViewModels;

namespace PPBM.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register PPBM application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all PPBM application services, ViewModels, and windows with appropriate lifetimes.
    /// </summary>
    /// <param name="services">The service collection to register with.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddProjectServices(this IServiceCollection services)
    {
        // Services — singleton (stateless wrappers around system APIs)
        services.AddSingleton<IPowerConfigService, PowerConfigService>();
        services.AddSingleton<IMonitorService, MonitorService>();
        services.AddSingleton<IScheduledTaskService, ScheduledTaskService>();

        // ViewModel — singleton (single-window application)
        services.AddSingleton<MainViewModel>();

        // Window — transient (created once, but lifetime managed by WPF)
        services.AddTransient<MainWindow>();

        return services;
    }
}
