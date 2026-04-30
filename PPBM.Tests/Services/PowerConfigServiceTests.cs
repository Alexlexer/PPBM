using Moq;
using PPBM.Contracts;
using PPBM.Models;
using PPBM.Services;
using Xunit;

namespace PPBM.Tests.Services;

public class PowerConfigServiceTests
{
    [Fact]
    public void Implements_IPowerConfigService()
    {
        var service = new PowerConfigService();
        Assert.IsAssignableFrom<IPowerConfigService>(service);
    }
}
