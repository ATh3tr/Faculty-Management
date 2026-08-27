using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FacultyManagement.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<HealthEndpointTests.Factory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(Factory factory) => _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task Health_is_public_and_healthy()
    {
        var response = await _client.GetAsync("/health");
        Assert.True(response.IsSuccessStatusCode);
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:SeedOnStartup"] = "false",
                ["Jwt:SigningKey"] = "integration-test-signing-key-with-at-least-32-characters"
            }));
    }
}
