using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PPBM.Models;

/// <summary>
/// Represents a navigation item in the sidebar pane.
/// </summary>
public class NavItem : INotifyPropertyChanged
{
    private bool _isActive;

    /// <summary>The page identifier this item navigates to.</summary>
    public string PageName { get; init; } = "";

    /// <summary>Display label for the navigation item.</summary>
    public string Label { get; init; } = "";

    /// <summary>Segoe MDL2 Assets glyph character for the icon.</summary>
    public string IconGlyph { get; init; } = "";

    /// <summary>Whether this nav item is currently active.</summary>
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(); }
    }

    /// <summary>Whether this item belongs to the bottom section of the nav pane.</summary>
    public bool IsBottom { get; init; }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
