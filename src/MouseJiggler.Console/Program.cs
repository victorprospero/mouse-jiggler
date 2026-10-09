using Microsoft.Extensions.Hosting;
using MouseJiggler.ConsoleApp;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    // appsettings.json is copied next to the binary, so load it from there regardless of the working directory.
    ContentRootPath = AppContext.BaseDirectory,
});

builder.AddMouseJiggler();

using var host = builder.Build();
return await host.RunMouseJigglerAsync(args);
