using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using MouseJiggler.Application.Ports;
using MouseJiggler.Infrastructure.MacOS;

namespace MouseJiggler.Infrastructure.IntegrationTests;

public sealed class DependencyInjectionTests
{
    [MacOSFact]
    [SupportedOSPlatform("macos")]
    public void AddInfrastructure_OnMacOS_RegistersTheCoreGraphicsAdapters()
    {
        using var provider = new ServiceCollection().AddInfrastructure().BuildServiceProvider();

        provider.GetRequiredService<IMouseController>().ShouldBeOfType<MacMouseController>();
        provider.GetRequiredService<IPointerReadiness>().ShouldBeOfType<MacPointerReadiness>();
        provider.GetService<UnsupportedPlatformPointer>().ShouldBeNull();
    }
}
