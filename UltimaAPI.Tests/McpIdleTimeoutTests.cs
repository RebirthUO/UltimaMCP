using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using Xunit;

namespace UltimaAPI.Tests;

public sealed class McpIdleTimeoutTests(UltimaApiFactory factory) : IClassFixture<UltimaApiFactory>
{
    [Fact]
    public void HttpTransportIdleTimeoutIsTwentyFourHours()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        HttpServerTransportOptions options = scope.ServiceProvider
            .GetRequiredService<IOptions<HttpServerTransportOptions>>()
            .Value;

        Assert.Equal(TimeSpan.FromHours(24), options.IdleTimeout);
    }
}
