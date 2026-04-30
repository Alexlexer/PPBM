using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using PPBM.Models;
using PPBM.Services;
using WColor = System.Windows.Media.Color;

namespace PPBM.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly System.Timers.Timer _pollTimer;
    private PerformanceCounter[] _thermalCounters = [];
    private bool _thermalReady;

    private static readonly SolidColorBrush TempCold = new(WColor.FromRgb(0x6B, 0xCB, 0x77));
    private static readonly SolidColorBrush TempWarm = new(WColor.FromRgb(0xFF, 0xD1, 0x66));
    private static readonly SolidColorBrush TempHot = new(WColor.FromRgb(0xFF, 0x8C, 0x42));
    private static readonly SolidColorBrush TempCritical = new(WColor.FromRgb(0xE0, 0x43, 0x43));
    private static readonly SolidColorBrush BoostNormal = new(WColor.FromRgb(0x60, 0xCD, 0xFF));
    private static readonly SolidColorBrush BoostDisabled = new(WColor.FromRgb(0x6B, 0xCB, 0x77));
    private static readonly SolidColorBrush BoostAggressive = new(WColor.FromRgb(0xE0, 0x43, 0x43));

    public MainViewModel()
    {
        Profiles = [.. PowerProfile.All];
        SelectedProfile = PowerProfile.Disabled;

        RefreshBoostModeCommand = new RelayCommand(async _ => await RefreshAsync());
        ApplyProfileCommand = new RelayCommand(async _ => await ApplyProfileAsync());
        AutoFixCommand = new RelayCommand(async _ => await AutoFixAsync());
        UnhideCommand = new RelayCommand(async _ => await UnhideAsync());
        ToggleSurviveUpdatesCommand = new RelayCommand(async _ => await ToggleSurviveUpdatesAsync());
        SelectProfileCommand = new RelayCommand(obj => { if (obj is PowerProfile p) SelectedProfile = p; return Task.CompletedTask; });

        _pollTimer = new System.Timers.Timer(2000);
        _pollTimer.Elapsed += async (_, _) =>
        {
            try
            {
                await PollTempsAsync();
            }
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
                    pc.NextValue(); // warm up
                    _thermalCounters[i] = pc;
                }
                _thermalReady = true;
            }
            catch { _thermalReady = true; }
        });

        _pollTimer.Start();

        _ = RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PowerProfile> Profiles
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    }

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

    public bool IsBusy
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    } = "Ready";

    public BoostMode CurrentBoostMode
    {
        get => field;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BoostModeName));
            OnPropertyChanged(nameof(IsBoostModeBad));
            OnPropertyChanged(nameof(BoostModeBrush));
        }
    }

    public string BoostModeName => CurrentBoostMode switch
    {
        BoostMode.Disabled => "Disabled (Cool & Quiet)",
        BoostMode.Enabled => "Enabled (Balanced)",
        BoostMode.Aggressive => "Aggressive (HOT)",
        BoostMode.EfficientEnabled => "Efficient Enabled (Gaming)",
        BoostMode.EfficientAggressive => "Efficient Aggressive (Rendering)",
        _ => "Unknown"
    };

    public SolidColorBrush BoostModeBrush => CurrentBoostMode switch
    {
        BoostMode.Aggressive => BoostAggressive,
        BoostMode.Disabled => BoostDisabled,
        _ => BoostNormal
    };

    public bool IsBoostModeBad => CurrentBoostMode == BoostMode.Aggressive;

    public bool IsAggressiveDetected
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    }

    public string CpuName
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    } = "Detecting...";

    public float? PackageTemp
    {
        get => field;
        set { field = value; OnPropertyChanged(); OnPropertyChanged(nameof(CoreTempDisplay)); UpdateTempDisplay(); }
    }

    public float? MaxCoreTemp
    {
        get => field;
        set { field = value; OnPropertyChanged(); OnPropertyChanged(nameof(CoreTempDisplay)); UpdateTempDisplay(); }
    }

    public float? CpuLoad
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    }

    public SolidColorBrush TempColorBrush
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    } = new(WColor.FromRgb(0x99, 0x99, 0x99));

    public string TempDescription
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    } = "--";

    public bool SurviveUpdatesEnabled
    {
        get => field;
        set { field = value; OnPropertyChanged(); OnPropertyChanged(nameof(SurviveButtonText)); }
    }

    public string SurviveButtonText => SurviveUpdatesEnabled
        ? "Survive Updates: ON"
        : "Enable Survive Updates";

    public string BoostHexValue
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    } = "0x00000002";

    public int MaxCpuPercent
    {
        get => field;
        set { field = Math.Clamp(value, 50, 100); OnPropertyChanged(); }
    }

    public bool MaxCpuEnabled
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    }

    private ObservableCollection<MonitorInfo> _monitors = [];

    public ObservableCollection<MonitorInfo> Monitors
    {
        get => _monitors;
        set { _monitors = value; OnPropertyChanged(); }
    }

    public ICommand RefreshBoostModeCommand { get; }
    public ICommand ApplyProfileCommand { get; }
    public ICommand AutoFixCommand { get; }
    public ICommand UnhideCommand { get; }
    public ICommand ToggleSurviveUpdatesCommand { get; }
    public ICommand SelectProfileCommand { get; }

    public string CoreTempDisplay
    {
        get
        {
            var t = PackageTemp ?? MaxCoreTemp;
            return t is null ? "--°" : $"{t.Value:F0}°";
        }
    }

    private async Task RefreshAsync()
    {
        IsBusy = true;
        StatusMessage = "Checking current settings...";

        var mode = await PowerConfigService.GetCurrentBoostModeAsync();
        CurrentBoostMode = mode;
        IsAggressiveDetected = mode == BoostMode.Aggressive;
        BoostHexValue = $"0x{((int)mode):X8}";

        var monitors = await Task.Run(MonitorService.GetMonitors);
        var surviveEnabled = await Task.Run(ScheduledTaskService.IsTaskInstalled);
        Monitors = [.. monitors];
        SurviveUpdatesEnabled = surviveEnabled;

        await PollTempsAsync();

        StatusMessage = IsAggressiveDetected
            ? "Aggressive boost mode detected -- causing high idle temps!"
            : $"Current mode: {BoostModeName} -- all good!";

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

        var boostOk = await PowerConfigService.SetBoostModeAsync(SelectedProfile.BoostMode);
        var freqOk = MaxCpuEnabled && await PowerConfigService.SetMaxCpuFrequencyAsync(MaxCpuPercent);

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

        var curMode = await PowerConfigService.GetCurrentBoostModeAsync();
        if (curMode != BoostMode.Aggressive)
        {
            StatusMessage = "Boost mode is already not Aggressive. No fix needed.";
            IsBusy = false;
            return;
        }

        var success = await PowerConfigService.SetBoostModeAsync(BoostMode.Disabled);
        if (success && MaxCpuEnabled)
            await PowerConfigService.SetMaxCpuFrequencyAsync(MaxCpuPercent);

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

        var success = await PowerConfigService.UnhideBoostSettingAsync();
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
            ScheduledTaskService.UninstallTask();
            SurviveUpdatesEnabled = false;
            StatusMessage = "Survive updates task removed.";
        }
        else
        {
            var success = await ScheduledTaskService.InstallTaskAsync(
                SelectedProfile.BoostMode,
                MaxCpuEnabled ? MaxCpuPercent : 100);

            SurviveUpdatesEnabled = success;
            StatusMessage = success
                ? "Will auto-reapply after Windows updates & login."
                : "Failed to create task.";
        }

        IsBusy = false;
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class RelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private bool _isExecuting;

    public RelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter)
    {
        _isExecuting = true;
        CommandManager.InvalidateRequerySuggested();
        try { await _execute(parameter); }
        finally
        {
            _isExecuting = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
