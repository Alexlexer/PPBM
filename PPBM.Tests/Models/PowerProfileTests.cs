using PPBM.Models;
using Xunit;

namespace PPBM.Tests.Models;

public class PowerProfileTests
{
    [Fact]
    public void AllProfiles_Contains_FiveProfiles()
    {
        Assert.Equal(5, PowerProfile.All.Length);
    }

    [Fact]
    public void DisabledProfile_HasRecommendedFlag()
    {
        Assert.True(PowerProfile.Disabled.IsRecommended);
    }

    [Fact]
    public void EfficientEnabledProfile_HasRecommendedFlag()
    {
        Assert.True(PowerProfile.EfficientEnabled.IsRecommended);
    }

    [Fact]
    public void Profiles_Have_DistinctNames()
    {
        var names = PowerProfile.All.Select(p => p.Name).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void SelectedProfile_Sets_IsSelected_Flag()
    {
        var profile = new PowerProfile { Name = "Test" };
        Assert.False(profile.IsSelected);
        profile.IsSelected = true;
        Assert.True(profile.IsSelected);
    }

    [Fact]
    public void SelectedProfile_Notifies_PropertyChanged()
    {
        var profile = new PowerProfile { Name = "Test" };
        var eventRaised = false;
        profile.PropertyChanged += (_, _) => eventRaised = true;
        profile.IsSelected = true;
        Assert.True(eventRaised);
    }

    [Fact]
    public void DisabledProfile_UsesBoostModeDisabled()
    {
        Assert.Equal(BoostMode.Disabled, PowerProfile.Disabled.BoostMode);
    }

    [Fact]
    public void AggregateProfile_UsesBoostModeAggressive()
    {
        Assert.Equal(BoostMode.Aggressive, PowerProfile.Aggressive.BoostMode);
    }
}
