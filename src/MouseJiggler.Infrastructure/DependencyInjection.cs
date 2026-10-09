using Microsoft.Extensions.DependencyInjection;
using MouseJiggler.Application.Ports;
using MouseJiggler.Infrastructure.MacOS;

namespace MouseJiggler.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the pointer adapters for the current operating system.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        if (OperatingSystem.IsMacOS())
        {
            services.AddSingleton<IMouseController, MacMouseController>();
            services.AddSingleton<IPointerReadiness, MacPointerReadiness>();
            return services;
        }

        services.AddSingleton<UnsupportedPlatformPointer>();
        services.AddSingleton<IMouseController>(sp => sp.GetRequiredService<UnsupportedPlatformPointer>());
        services.AddSingleton<IPointerReadiness>(sp => sp.GetRequiredService<UnsupportedPlatformPointer>());
        return services;
    }
}
