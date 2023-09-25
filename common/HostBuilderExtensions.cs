using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dapr.Tests.Common;

public static class HostBuilderExtensions
{
    public static IHostBuilder ConfigureTestInfraLogging(this IHostBuilder builder)
    {
        return builder.ConfigureLogging(
            (hostingContext, config) =>
            {
                config.ClearProviders();
                config.AddJsonConsole();
            });
    }
}
