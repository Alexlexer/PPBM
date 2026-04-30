using System.Text.Json;
using PPBM.Models;
using Xunit;

namespace PPBM.Tests.Models;

public class MonitorInfoSerializationTests
{
    [Fact]
    public void MonitorInfo_Serializes_And_Deserializes()
    {
        var original = new MonitorInfo("DISPLAY1", true, "DisplayPort", 144, true);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<MonitorInfo>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.IsInternal, deserialized.IsInternal);
        Assert.Equal(original.ConnectionType, deserialized.ConnectionType);
        Assert.Equal(original.CurrentRefreshRate, deserialized.CurrentRefreshRate);
        Assert.Equal(original.IsAbove60Hz, deserialized.IsAbove60Hz);
    }

    [Fact]
    public void RefreshWarning_WhenAbove60Hz_ReturnsWarning()
    {
        var monitor = new MonitorInfo("Test", false, "HDMI", 75, true);
        Assert.Contains("Warning", monitor.RefreshWarning);
    }

    [Fact]
    public void RefreshWarning_WhenAt60Hz_ReturnsOK()
    {
        var monitor = new MonitorInfo("Test", false, "HDMI", 60, false);
        Assert.Equal("OK", monitor.RefreshWarning);
    }
}
