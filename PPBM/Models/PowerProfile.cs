using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PPBM.Models;

public class PowerProfile : INotifyPropertyChanged
{
    private bool _isSelected;

    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public BoostMode BoostMode { get; init; }
    public bool IsRecommended { get; init; }
    public string UseCase { get; init; } = "";
    public string TempLabel { get; init; } = "";

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public static readonly PowerProfile Disabled = new()
    {
        Name = "Cool & Quiet",
        Description = "No boost, lowest temps — best for browsing, coding, Office, multi-monitor",
        BoostMode = BoostMode.Disabled,
        IsRecommended = true,
        UseCase = "Productivity / Daily Use",
        TempLabel = "Coldest"
    };

    public static readonly PowerProfile Enabled = new()
    {
        Name = "Balanced",
        Description = "Light boost when needed — general use",
        BoostMode = Models.BoostMode.Enabled,
        IsRecommended = false,
        UseCase = "Balanced General Use",
        TempLabel = "Cool"
    };

    public static readonly PowerProfile Aggressive = new()
    {
        Name = "Aggressive (Factory Default)",
        Description = "Constant boost — causes overheating on multi-monitor setups",
        BoostMode = Models.BoostMode.Aggressive,
        IsRecommended = false,
        UseCase = "Not recommended",
        TempLabel = "HOT"
    };

    public static readonly PowerProfile EfficientEnabled = new()
    {
        Name = "Gaming Optimized",
        Description = "Smart boost for gaming — 97-99% FPS with 15-20°C cooler",
        BoostMode = Models.BoostMode.EfficientEnabled,
        IsRecommended = true,
        UseCase = "Gaming",
        TempLabel = "~5°C above Disabled"
    };

    public static readonly PowerProfile EfficientAggressive = new()
    {
        Name = "Rendering / Compilation",
        Description = "Full boost when needed, but smarter than Aggressive",
        BoostMode = Models.BoostMode.EfficientAggressive,
        IsRecommended = false,
        UseCase = "Heavy Rendering / Compilation",
        TempLabel = "Warm"
    };

    public static readonly PowerProfile[] All = [Disabled, Enabled, Aggressive, EfficientEnabled, EfficientAggressive];
}
