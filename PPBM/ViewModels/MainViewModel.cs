using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using PPBM.Models;
using PPBM.Services;
using WColor = System.Windows.Media.Color;

namespace PPBM.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly CpuTemperatureService _cpuService;
    private readonly System.Timers.Timer _pollTimer;
    private bool _isBusy;
    private string _statusMessage = "Ready";
    private BoostMode _currentBoostMode = BoostMode.Aggressive;
    private PowerProfile _selectedProfile = PowerProfile.Disabled;
    private bool _isAggressiveDetected;
    private string _cpuName = "Detecting...";
    private float? _packageTemp;
    private float? _maxCoreTemp;
    private float? _cpuLoad;
    private string _tempDescription = "--";
    private bool _surviveUpdatesEnabled;
    private string _boostHexValue = "0x00000002";
    private int _maxCpuPercent = 100;
    private bool _maxCpuEnabled;
    private ObservableCollection<MonitorInfo> _monitors = [];
    private ObservableCollection<PowerProfile> _profiles;
    private SolidColorBrush _tempColorBrush = new(WColor.FromRgb(0x88, 0x88, 0x88));

    private static readonly SolidColorBrush TempCold = new(WColor.FromRgb(0x4C, 0xAF, 0x50));
    private static readonly SolidColorBrush TempWarm = new(WColor.FromRgb(0xFF, 0xC1, 0x07));
    private static readonly SolidColorBrush TempHot = new(WColor.FromRgb(0xFF, 0x98, 0x00));
    private static readonly SolidColorBrush TempCritical = new(WColor.FromRgb(0xF4, 0x43, 0x36));
    private static readonly SolidColorBrush BoostNormal = new(WColor.FromRgb(0xFA, 0xB3, 0x87));
    private static readonly SolidColorBrush BoostDisabled = new(WColor.FromRgb(0xA6, 0xE3, 0xA1));
    private static readonly SolidColorBrush BoostAggressive = new(WColor.FromRgb(0xF3, 0x8B, 0xA8));

    public MainViewModel()
    {
        _cpuService = new CpuTemperatureService();
        _profiles = new ObservableCollection<PowerProfile>(PowerProfile.All);
        SelectedProfile = PowerProfile.Disabled;

        RefreshBoostModeCommand = new RelayCommand(async _ => await RefreshAsync());
        ApplyProfileCommand = new RelayCommand(async _ => await ApplyProfileAsync());
        AutoFixCommand = new RelayCommand(async _ => await AutoFixAsync());
        UnhideCommand = new RelayCommand(async _ => await UnhideAsync());
        ToggleSurviveUpdatesCommand = new RelayCommand(async _ => await ToggleSurviveUpdatesAsync());
        SelectProfileCommand = new RelayCommand(obj => { if (obj is PowerProfile p) SelectedProfile = p; return Task.CompletedTask; });

        _pollTimer = new System.Timers.Timer(2000);
        _pollTimer.Elapsed += (_, _) => System.Windows.Application.Current.Dispatcher.Invoke(() => UpdateTemps());
        _pollTimer.AutoReset = true;
        _pollTimer.Start();

        _ = RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PowerProfile> Profiles
    {
        get => _profiles;
        set { _profiles = value; OnPropertyChanged(); }
    }

    public PowerProfile SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (_selectedProfile == value) return;
            if (_selectedProfile != null) _selectedProfile.IsSelected = false;
            _selectedProfile = value;
            if (_selectedProfile != null) _selectedProfile.IsSelected = true;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public BoostMode CurrentBoostMode
    {
        get => _currentBoostMode;
        set { _currentBoostMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(BoostModeName)); OnPropertyChanged(nameof(IsBoostModeBad)); OnPropertyChanged(nameof(BoostModeBrush)); }
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
        get => _isAggressiveDetected;
        set { _isAggressiveDetected = value; OnPropertyChanged(); }
    }

    public string CpuName
    {
        get => _cpuName;
        set { _cpuName = value; OnPropertyChanged(); }
    }

    public float? PackageTemp
    {
        get => _packageTemp;
        set { _packageTemp = value; OnPropertyChanged(); OnPropertyChanged(nameof(CoreTempDisplay)); UpdateTempDisplay(); }
    }

    public float? MaxCoreTemp
    {
        get => _maxCoreTemp;
        set { _maxCoreTemp = value; OnPropertyChanged(); OnPropertyChanged(nameof(CoreTempDisplay)); UpdateTempDisplay(); }
    }

    public float? CpuLoad
    {
        get => _cpuLoad;
        set { _cpuLoad = value; OnPropertyChanged(); }
    }

    public SolidColorBrush TempColorBrush
    {
        get => _tempColorBrush;
        set { _tempColorBrush = value; OnPropertyChanged(); }
    }

    public string TempDescription
    {
        get => _tempDescription;
        set { _tempDescription = value; OnPropertyChanged(); }
    }

    public bool SurviveUpdatesEnabled
    {
        get => _surviveUpdatesEnabled;
        set { _surviveUpdatesEnabled = value; OnPropertyChanged(); OnPropertyChanged(nameof(SurviveButtonText)); }
    }

    public string SurviveButtonText => SurviveUpdatesEnabled
        ? "Survive Updates: ON"
        : "Enable Survive Updates";

    public string BoostHexValue
    {
        get => _boostHexValue;
        set { _boostHexValue = value; OnPropertyChanged(); }
    }

    public int MaxCpuPercent
    {
        get => _maxCpuPercent;
        set { _maxCpuPercent = Math.Clamp(value, 50, 100); OnPropertyChanged(); }
    }

    public bool MaxCpuEnabled
    {
        get => _maxCpuEnabled;
        set { _maxCpuEnabled = value; OnPropertyChanged(); }
    }

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
            if (t is null) return "--°";
            return $"{t.Value:F0}°";
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

        var monitors = MonitorService.GetMonitors();
        Monitors = new ObservableCollection<MonitorInfo>(monitors);

        SurviveUpdatesEnabled = ScheduledTaskService.IsTaskInstalled();

        if (IsAggressiveDetected)
            StatusMessage = "Aggressive boost mode detected -- causing high idle temps!";
        else
            StatusMessage = $"Current mode: {BoostModeName} -- all good!";

        IsBusy = false;
    }

    private void UpdateTemps()
    {
        var (package, maxCore, name) = _cpuService.GetCpuTemperatures();
        CpuName = name;
        PackageTemp = package;
        MaxCoreTemp = maxCore;

        var load = _cpuService.GetCpuLoad();
        CpuLoad = load;
    }

    private void UpdateTempDisplay()
    {
        var temp = PackageTemp ?? MaxCoreTemp;
        if (temp is null)
        {
            TempDescription = "N/A";
            TempColorBrush = new SolidColorBrush(WColor.FromRgb(0x88, 0x88, 0x88));
            return;
        }

        var t = temp.Value;
        SolidColorBrush brush;
        if (t < 50)
            brush = TempCold;
        else if (t < 70)
            brush = TempWarm;
        else if (t < 85)
            brush = TempHot;
        else
            brush = TempCritical;

        TempColorBrush = brush;
        TempDescription = $"{t:F0}C";
    }

    private async Task ApplyProfileAsync()
    {
        IsBusy = true;
        StatusMessage = $"Applying {SelectedProfile.Name}...";

        var success = await PowerConfigService.SetBoostModeAsync(SelectedProfile.BoostMode);
        if (success && MaxCpuEnabled)
            await PowerConfigService.SetMaxCpuFrequencyAsync(MaxCpuPercent);

        if (success)
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
