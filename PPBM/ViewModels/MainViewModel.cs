using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using PPBM.Contracts;
using PPBM.Models;
using WColor = System.Windows.Media.Color;

namespace PPBM.ViewModels;

/// <summary>
/// Primary ViewModel for the PPBM main window. Manages navigation, power profiles,
/// CPU thermal monitoring, boost mode detection, monitor enumeration, and scheduled task persistence.
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly IPowerConfigService _powerConfig;
    private readonly IMonitorService _monitor;
    private readonly IScheduledTaskService _scheduledTask;
    private readonly System.Timers.Timer _pollTimer;
    private PerformanceCounter[] _thermalCounters = [];
    private bool _thermalReady;

    private static readonly SolidColorBrush TempCold = new(WColor.FromRgb(0xC0, 0xC0, 0xC0));
    private static readonly SolidColorBrush TempWarm = new(WColor.FromRgb(0xE0, 0xE0, 0xE0));
    private static readonly SolidColorBrush TempHot = new(WColor.FromRgb(0xF0, 0xF0, 0xF0));
    private static readonly SolidColorBrush TempCritical = new(WColor.FromRgb(0xF0, 0xF0, 0xF0));
    private static readonly SolidColorBrush BoostDotOk = new(WColor.FromRgb(0xB0, 0xB0, 0xB0));
    private static readonly SolidColorBrush BoostDotWarn = new(WColor.FromRgb(0x80, 0x80, 0x80));
    private static readonly SolidColorBrush BoostDotDanger = new(WColor.FromRgb(0xF0, 0xF0, 0xF0));

    /// <summary>
    /// Initializes a new instance of <see cref="MainViewModel"/> using injected services.
    /// </summary>
    public MainViewModel(IPowerConfigService powerConfig, IMonitorService monitor, IScheduledTaskService scheduledTask)
    {
        _powerConfig = powerConfig;
        _monitor = monitor;
        _scheduledTask = scheduledTask;

        Profiles = [.. PowerProfile.All];
        SelectedProfile = PowerProfile.Disabled;

        RefreshBoostModeCommand = new RelayCommand(async _ => await RefreshAsync());
        ApplyProfileCommand = new RelayCommand(async _ => await ApplyProfileAsync());
        AutoFixCommand = new RelayCommand(async _ => await AutoFixAsync());
        UnhideCommand = new RelayCommand(async _ => await UnhideAsync());
        ToggleSurviveUpdatesCommand = new RelayCommand(async _ => await ToggleSurviveUpdatesAsync());
        SelectProfileCommand = new RelayCommand(obj => { if (obj is PowerProfile p) SelectedProfile = p; return Task.CompletedTask; });
        NavigateCommand = new RelayCommand(obj =>
        {
            if (obj is string page) ActivePage = page;
            return Task.CompletedTask;
        });
        OpenLogCommand = new RelayCommand(_ =>
        {
            var logPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PPBM", "debug.log");
            try
            {
                if (System.IO.File.Exists(logPath))
                    Process.Start(new ProcessStartInfo(logPath) { UseShellExecute = true });
            }
            catch { }
            return Task.CompletedTask;
        });

        NavigationItems =
        [
            new NavItem { PageName = "Dashboard", Label = "Dashboard", IconGlyph = "\uEC00" },
            new NavItem { PageName = "Profiles", Label = "Power Profiles", IconGlyph = "\uE8FD" },
            new NavItem { PageName = "Monitors", Label = "Monitors", IconGlyph = "\uE7F4" },
            new NavItem { PageName = "Utilities", Label = "Utilities", IconGlyph = "\uE713" },
            new NavItem { PageName = "About", Label = "About", IconGlyph = "\uE946", IsBottom = true }
        ];
        ActivePage = "Dashboard";

        _pollTimer = new System.Timers.Timer(2000);
        _pollTimer.Elapsed += async (_, _) =>
        {
            try { await PollTempsAsync(); }
            catch { }
        };
        _pollTimer.AutoReset = true;

        _ = Task.Run(() =>
        {
            try
            {
                var names = new PerformanceCounterCategory("Thermal Zone Information").GetInstanceNames();
                _thermalCounters = new PerformanceCounter[names.Length];
                for (int i = 0; i < names.Length; i++)
                {
                    var pc = new PerformanceCounter("Thermal Zone Information", "Temperature", names[i], true);
                    pc.NextValue();
                    _thermalCounters[i] = pc;
                }
                _thermalReady = true;
            }
            catch { _thermalReady = true; }
        });

        _pollTimer.Start();
        _ = RefreshAsync();
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Available navigation items for the sidebar.</summary>
    public ObservableCollection<NavItem> NavigationItems { get; }

    /// <summary>The currently active page name.</summary>
    public string ActivePage
    {
        get => field;
        set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDashboardVisible));
            OnPropertyChanged(nameof(IsProfilesVisible));
            OnPropertyChanged(nameof(IsMonitorsVisible));
            OnPropertyChanged(nameof(IsUtilitiesVisible));
            foreach (var item in NavigationItems)
                item.IsActive = item.PageName == value;
        }
    }

    /// <summary>Visibility helper for Dashboard page.</summary>
    public System.Windows.Visibility IsDashboardVisible => ActivePage == "Dashboard"
        ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    /// <summary>Visibility helper for Profiles page.</summary>
    public System.Windows.Visibility IsProfilesVisible => ActivePage == "Profiles"
        ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    /// <summary>Visibility helper for Monitors page.</summary>
    public System.Windows.Visibility IsMonitorsVisible => ActivePage == "Monitors"
        ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    /// <summary>Visibility helper for Utilities page.</summary>
    public System.Windows.Visibility IsUtilitiesVisible => ActivePage == "Utilities"
        ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    /// <summary>Status pill text for boost mode indicator.</summary>
    public string BoostStatusText => CurrentBoostMode switch
    {
        BoostMode.Aggressive => "Aggressive Mode Active",
        BoostMode.Disabled => "Disabled — Cool & Quiet",
        _ => $"{BoostModeName} Active"
    };

    /// <summary>Status pill dot color for boost mode.</summary>
    public SolidColorBrush BoostStatusDot => CurrentBoostMode switch
    {
        BoostMode.Aggressive => new SolidColorBrush(WColor.FromRgb(0xF0, 0xF0, 0xF0)),
        BoostMode.Disabled => new SolidColorBrush(WColor.FromRgb(0xC0, 0xC0, 0xC0)),
        _ => new SolidColorBrush(WColor.FromRgb(0x80, 0x80, 0x80))
    };

    /// <summary>Commands and data properties — see original for full listing.</summary>

    /// <summary>Available power profiles for selection.</summary>
    public ObservableCollection<PowerProfile> Profiles
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    }

    /// <summary>The currently selected power profile.</summary>
    public PowerProfile SelectedProfile
    {
        get => field;
        set
        {
            if (field == value) return;
            if (field is not null) field.IsSelected = false;
            field = value;
            if (value is not null) value.IsSelected = true;
            OnPropertyChanged();
        }
    }

    /// <summary>Indicates whether a long-running operation is in progress.</summary>
    public bool IsBusy
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    }

    /// <summary>Status bar message for user feedback.</summary>
    public string StatusMessage
    {
        get => field;
        set { field = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusTimestamp)); }
    } = "Ready";

    /// <summary>Timestamp for last status update.</summary>
    public string StatusTimestamp => $"Ready · Last refreshed {DateTime.Now:HH:mm:ss}";

    /// <summary>Current Processor Performance Boost Mode.</summary>
    public BoostMode CurrentBoostMode
    {
        get => field;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BoostModeName));
            OnPropertyChanged(nameof(IsBoostModeBad));
            OnPropertyChanged(nameof(BoostDotColor));
            OnPropertyChanged(nameof(BoostDotClass));
            OnPropertyChanged(nameof(BoostStatusText));
            OnPropertyChanged(nameof(BoostStatusDot));
        }
    }

    /// <summary>Human-readable name of the current boost mode.</summary>
    public string BoostModeName => CurrentBoostMode switch
    {
        BoostMode.Disabled => "Disabled",
        BoostMode.Enabled => "Enabled",
        BoostMode.Aggressive => "Aggressive",
        BoostMode.EfficientEnabled => "Efficient Enabled",
        BoostMode.EfficientAggressive => "Efficient Aggressive",
        _ => "Unknown"
    };

    /// <summary>Dot color for the boost mode indicator in hero card.</summary>
    public SolidColorBrush BoostDotColor => CurrentBoostMode switch
    {
        BoostMode.Aggressive => BoostDotDanger,
        BoostMode.Disabled => BoostDotOk,
        _ => BoostDotWarn
    };

    /// <summary>CSS-style class for boost dot (used in template triggers).</summary>
    public string BoostDotClass => CurrentBoostMode switch
    {
        BoostMode.Aggressive => "danger",
        BoostMode.Disabled => "ok",
        _ => "warn"
    };

    /// <summary>Indicates whether the current boost mode is the problematic Aggressive mode.</summary>
    public bool IsBoostModeBad => CurrentBoostMode == BoostMode.Aggressive;

    /// <summary>Indicates whether Aggressive boost mode was detected.</summary>
    public bool IsAggressiveDetected
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    }

    /// <summary>Name of the detected CPU.</summary>
    public string CpuName
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    } = "Detecting...";

    /// <summary>Package-level temperature in Celsius.</summary>
    public float? PackageTemp
    {
        get => field;
        set { field = value; OnPropertyChanged(); OnPropertyChanged(nameof(CoreTempDisplay)); OnPropertyChanged(nameof(TempColor)); OnPropertyChanged(nameof(TempClass)); UpdateTempDisplay(); }
    }

    /// <summary>Maximum core temperature in Celsius.</summary>
    public float? MaxCoreTemp
    {
        get => field;
        set { field = value; OnPropertyChanged(); OnPropertyChanged(nameof(CoreTempDisplay)); OnPropertyChanged(nameof(TempColor)); OnPropertyChanged(nameof(TempClass)); UpdateTempDisplay(); }
    }

    /// <summary>Temperature color brush for hero display.</summary>
    public SolidColorBrush TempColor => (PackageTemp ?? MaxCoreTemp) switch
    {
        null => new SolidColorBrush(WColor.FromRgb(0xC0, 0xC0, 0xC0)),
        < 50 => TempCold,
        < 70 => TempWarm,
        < 85 => TempHot,
        _ => TempCritical
    };

    /// <summary>Temperature CSS-style class.</summary>
    public string TempClass => (PackageTemp ?? MaxCoreTemp) switch
    {
        null => "cool",
        < 50 => "cool",
        < 70 => "warm",
        _ => "hot"
    };

    /// <summary>Current CPU load as a percentage.</summary>
    public float? CpuLoad
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    }

    /// <summary>Color brush indicating temperature severity.</summary>
    public SolidColorBrush TempColorBrush
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    } = new(WColor.FromRgb(0x99, 0x99, 0x99));

    /// <summary>Human-readable temperature description.</summary>
    public string TempDescription
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    } = "--";

    /// <summary>Whether the "Survive Updates" scheduled task is installed.</summary>
    public bool SurviveUpdatesEnabled
    {
        get => field;
        set { field = value; OnPropertyChanged(); OnPropertyChanged(nameof(SurviveButtonText)); }
    }

    /// <summary>Label for the survive-updates toggle button.</summary>
    public string SurviveButtonText => SurviveUpdatesEnabled ? "Disable" : "Enable";

    /// <summary>Hex representation of the current boost mode value.</summary>
    public string BoostHexValue
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    } = "GUID: be337238-0d82-4146-a960-4f3749d470c7  ·  Value: 0x00000002";

    /// <summary>Maximum CPU frequency percentage (50-100).</summary>
    public int MaxCpuPercent
    {
        get => field;
        set { field = Math.Clamp(value, 50, 100); OnPropertyChanged(); OnPropertyChanged(nameof(SliderPercent)); }
    }

    /// <summary>Format string for slider fill.</summary>
    public double SliderPercent => ((MaxCpuPercent - 50.0) / 50.0) * 100.0;

    /// <summary>Whether the max CPU frequency limiter is enabled.</summary>
    public bool MaxCpuEnabled
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    }

    private ObservableCollection<MonitorInfo> _monitors = [];

    /// <summary>List of detected display monitors.</summary>
    public ObservableCollection<MonitorInfo> Monitors
    {
        get => _monitors;
        set { _monitors = value; OnPropertyChanged(); }
    }

    /// <summary>Command to refresh boost mode status.</summary>
    public ICommand RefreshBoostModeCommand { get; }

    /// <summary>Command to apply the selected power profile.</summary>
    public ICommand ApplyProfileCommand { get; }

    /// <summary>Command to auto-fix aggressive boost mode.</summary>
    public ICommand AutoFixCommand { get; }

    /// <summary>Command to unhide the boost setting in Windows UI.</summary>
    public ICommand UnhideCommand { get; }

    /// <summary>Command to toggle the survive-updates scheduled task.</summary>
    public ICommand ToggleSurviveUpdatesCommand { get; }

    /// <summary>Command to select a power profile.</summary>
    public ICommand SelectProfileCommand { get; }

    /// <summary>Command to switch pages via navigation.</summary>
    public ICommand NavigateCommand { get; }

    /// <summary>Command to open the debug log file.</summary>
    public ICommand OpenLogCommand { get; }

    /// <summary>Formatted temperature display string (e.g. "45°").</summary>
    public string CoreTempDisplay
    {
        get
        {
            var t = PackageTemp ?? MaxCoreTemp;
            return t is null ? "--°C" : $"{t.Value:F0}°C";
        }
    }

    private async Task RefreshAsync()
    {
        IsBusy = true;
        StatusMessage = "Checking current settings...";

        var mode = await _powerConfig.GetCurrentBoostModeAsync();
        CurrentBoostMode = mode;
        IsAggressiveDetected = mode == BoostMode.Aggressive;
        BoostHexValue = $"GUID: be337238-0d82-4146-a960-4f3749d470c7  ·  Value: 0x{((int)mode):X8}";

        var monitors = await Task.Run(_monitor.GetMonitors);
        var surviveEnabled = await Task.Run(_scheduledTask.IsTaskInstalled);
        Monitors = [.. monitors];
        SurviveUpdatesEnabled = surviveEnabled;

        await PollTempsAsync();

        StatusMessage = IsAggressiveDetected
            ? "Aggressive boost mode detected — high idle temps!"
            : "Ready";

        IsBusy = false;
    }

    private async Task PollTempsAsync()
    {
        string cpuName = "Detecting...";
        float? maxTemp = null;
        float? cpuLoad = null;

        try
        {
            await Task.Run(() =>
            {
                if (_thermalReady)
                {
                    foreach (var pc in _thermalCounters)
                    {
                        try
                        {
                            var kelvin = pc.NextValue();
                            var celsius = kelvin - 273.15f;
                            if (celsius is >= 0 and <= 150 && (maxTemp is null || celsius > maxTemp))
                                maxTemp = celsius;
                        }
                        catch { }
                    }
                }

                if (maxTemp is null)
                {
                    try
                    {
                        using var searcher = new System.Management.ManagementObjectSearcher(
                            @"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature");
                        foreach (System.Management.ManagementObject obj in searcher.Get())
                        {
                            try
                            {
                                var val = Convert.ToDouble(obj["CurrentTemperature"]);
                                var celsius = (float)((val / 10.0) - 273.15);
                                if (celsius is >= 0 and <= 150 && (maxTemp is null || celsius > maxTemp))
                                    maxTemp = celsius;
                            }
                            catch { }
                        }
                    }
                    catch { }
                }

                using var cpuSearcher = new System.Management.ManagementObjectSearcher(
                    "SELECT Name, LoadPercentage FROM Win32_Processor");
                foreach (System.Management.ManagementObject obj in cpuSearcher.Get())
                {
                    try
                    {
                        cpuName = obj["Name"]?.ToString() ?? "CPU";
                        var rawLoad = obj["LoadPercentage"];
                        if (rawLoad is not null)
                            cpuLoad = Convert.ToSingle(rawLoad);
                    }
                    catch { }
                }
            });
        }
        catch
        {
            cpuName = "N/A";
            maxTemp = null;
            cpuLoad = null;
        }

        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null) return;

        await dispatcher.InvokeAsync(() =>
        {
            CpuName = cpuName;
            PackageTemp = maxTemp;
            MaxCoreTemp = maxTemp;
            CpuLoad = cpuLoad;
        });
    }

    private void UpdateTempDisplay()
    {
        var temp = PackageTemp ?? MaxCoreTemp;
        if (temp is null)
        {
            TempDescription = "N/A";
            TempColorBrush = new SolidColorBrush(WColor.FromRgb(0x99, 0x99, 0x99));
            return;
        }

        TempColorBrush = temp.Value switch
        {
            < 50 => TempCold,
            < 70 => TempWarm,
            < 85 => TempHot,
            _ => TempCritical
        };
        TempDescription = $"{temp.Value:F0}C";
    }

    private async Task ApplyProfileAsync()
    {
        IsBusy = true;
        StatusMessage = $"Applying {SelectedProfile.Name}...";

        var boostOk = await _powerConfig.SetBoostModeAsync(SelectedProfile.BoostMode);
        var freqOk = MaxCpuEnabled && await _powerConfig.SetMaxCpuFrequencyAsync(MaxCpuPercent);

        if (boostOk)
        {
            StatusMessage = $"{SelectedProfile.Name} applied! Temps should drop.";
            await RefreshAsync();
        }
        else
            StatusMessage = "Failed to apply settings. Run as Administrator.";

        IsBusy = false;
    }

    private async Task AutoFixAsync()
    {
        IsBusy = true;
        StatusMessage = "Auto-fix: setting Cool & Quiet mode...";

        var curMode = await _powerConfig.GetCurrentBoostModeAsync();
        if (curMode != BoostMode.Aggressive)
        {
            StatusMessage = "Boost mode is already not Aggressive. No fix needed.";
            IsBusy = false;
            return;
        }

        var success = await _powerConfig.SetBoostModeAsync(BoostMode.Disabled);
        if (success && MaxCpuEnabled)
            await _powerConfig.SetMaxCpuFrequencyAsync(MaxCpuPercent);

        StatusMessage = success
            ? "Auto-fixed! Set to Cool & Quiet. Temps should drop 20-40C."
            : "Auto-fix failed. Run as Administrator.";

        await RefreshAsync();
        IsBusy = false;
    }

    private async Task UnhideAsync()
    {
        IsBusy = true;
        StatusMessage = "Unhiding boost mode setting in Windows UI...";

        var success = await _powerConfig.UnhideBoostSettingAsync();
        StatusMessage = success
            ? "Setting is now visible in Control Panel > Power Options > Advanced"
            : "Failed to unhide. Run as Administrator.";

        IsBusy = false;
    }

    private async Task ToggleSurviveUpdatesAsync()
    {
        IsBusy = true;

        if (SurviveUpdatesEnabled)
        {
            _scheduledTask.UninstallTask();
            SurviveUpdatesEnabled = false;
            StatusMessage = "Survive Updates disabled.";
        }
        else
        {
            var success = await _scheduledTask.InstallTaskAsync(
                SelectedProfile.BoostMode,
                MaxCpuEnabled ? MaxCpuPercent : 100);

            SurviveUpdatesEnabled = success;
            StatusMessage = success
                ? "Survive Updates enabled — scheduled task created."
                : "Failed to create task.";
        }

        IsBusy = false;
    }

    /// <summary>Raises the <see cref="PropertyChanged"/> event.</summary>
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
