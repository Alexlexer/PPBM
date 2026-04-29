namespace PPBM.Models;

public record PowerProfile(
    string Name,
    string Description,
    BoostMode BoostMode,
    bool IsRecommended,
    string UseCase,
    string TempLabel
)
{
    public static readonly PowerProfile Disabled = new(
        "Cool & Quiet",
        "No boost, lowest temps — best for browsing, coding, Office, multi-monitor",
        BoostMode.Disabled,
        true,
        "Productivity / Daily Use",
        "Coldest"
    );

    public static readonly PowerProfile Enabled = new(
        "Balanced",
        "Light boost when needed — general use",
        Models.BoostMode.Enabled,
        false,
        "Balanced General Use",
        "Cool"
    );

    public static readonly PowerProfile Aggressive = new(
        "⚠️ Aggressive (Factory Default)",
        "Constant boost — causes overheating on multi-monitor setups",
        Models.BoostMode.Aggressive,
        false,
        "❌ Not recommended",
        "🔥 HOT"
    );

    public static readonly PowerProfile EfficientEnabled = new(
        "Gaming Optimized",
        "Smart boost for gaming — 97-99% FPS with 15-20°C cooler",
        Models.BoostMode.EfficientEnabled,
        true,
        "Gaming",
        "~5°C above Disabled"
    );

    public static readonly PowerProfile EfficientAggressive = new(
        "Rendering / Compilation",
        "Full boost when needed, but smarter than Aggressive",
        Models.BoostMode.EfficientAggressive,
        false,
        "Heavy Rendering / Compilation",
        "Warm"
    );

    public static readonly PowerProfile[] All = [Disabled, Enabled, Aggressive, EfficientEnabled, EfficientAggressive];
}
