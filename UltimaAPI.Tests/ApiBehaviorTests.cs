using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace UltimaAPI.Tests;

public sealed class ApiBehaviorTests(UltimaApiFactory factory) : IClassFixture<UltimaApiFactory>
{
    [Fact]
    public async Task WhenDataPathIsMissingThenHealthReportsSdkUninitialized()
    {
        using HttpClient client = factory.CreateClient();

        JsonElement response = await client.GetFromJsonAsync<JsonElement>(
            "/api/sdk/health",
            TestContext.Current.CancellationToken);

        Assert.False(response.GetProperty("sdkInitialized").GetBoolean());
    }

    [Fact]
    public async Task WhenTileDataIsUnavailableThenSummaryReturnsServiceUnavailable()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/tiles/summary",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task WhenMapIdIsInvalidThenCoordinateReturnsBadRequest()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/maps/99/coordinates/0/0?includeStatics=true",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WhenSearchLimitExceedsMaximumThenSearchReturnsBadRequest()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/tiles/items/search?query=sword&offset=0&limit=101",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WhenDiagnosticsAreRequestedThenEndpointRemainsAvailableInDegradedMode()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/sdk/diagnostics",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
