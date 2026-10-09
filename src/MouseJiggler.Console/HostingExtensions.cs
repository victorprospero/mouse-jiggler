using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MouseJiggler.Application;
using MouseJiggler.ConsoleApp.Configuration;
using MouseJiggler.Infrastructure;

namespace MouseJiggler.ConsoleApp;

/// <summary>Composition root: wires configuration, logging and every layer, then runs the app.</summary>
internal static class HostingExtensions
{
    private const string LogTimestampFormat = "HH:mm:ss ";

    public static HostApplicationBuilder AddMouseJiggler(this HostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(console =>
        {
            console.SingleLine = true;
            console.TimestampFormat = LogTimestampFormat;
        });

        builder.Services
            .AddOptions<JigglerOptions>()
            .Bind(builder.Configuration.GetSection(JigglerOptions.SectionName))
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<JigglerOptions>, JigglerOptionsValidator>();

        builder.Services
            .AddApplication()
            .AddInfrastructure()
            .AddSingleton<IConsole, SystemConsole>()
            .AddSingleton<JigglerApp>();

        return builder;
    }

    /// <summary>
    /// Starts the host (so Ctrl+C is translated into ApplicationStopping), runs the app until it
    /// finishes or the host is asked to stop, then shuts the host down. Returns the exit code.
    /// </summary>
    public static async Task<int> RunMouseJigglerAsync(this IHost host, string[] args)
    {
        await host.StartAsync().ConfigureAwait(false);

        var stopping = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
        var exitCode = await host.Services.GetRequiredService<JigglerApp>().RunAsync(args, stopping).ConfigureAwait(false);

        await host.StopAsync().ConfigureAwait(false);
        return exitCode;
    }
}
