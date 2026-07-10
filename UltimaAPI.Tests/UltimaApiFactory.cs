using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace UltimaAPI.Tests;

public sealed class UltimaApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            Dictionary<string, string?> settings = new()
            {
                ["Ultima:DataPath"] = Path.Combine(Path.GetTempPath(), "UltimaMCP.Tests", Guid.NewGuid().ToString("N"))
            };
            configuration.AddInMemoryCollection(settings);
        });
    }
}
