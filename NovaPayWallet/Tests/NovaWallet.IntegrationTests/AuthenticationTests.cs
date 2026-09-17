using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace NovaWallet.IntegrationTests;

public class AuthenticationTests : IClassFixture<NovaWalletApiFactory>
{
    private readonly NovaWalletApiFactory _factory;

    public AuthenticationTests(NovaWalletApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Endpoints_Without_Bearer_Token_Return_Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/wallets/{Guid.NewGuid()}/balance");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Endpoints_With_Valid_Bearer_Token_Are_Reachable()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/wallets/{Guid.NewGuid()}/balance");

        // Not found (wallet does not exist) proves the request passed auth and reached the controller.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_Spec_Is_Reachable()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    internal static async Task<string> GetTokenAsync(HttpClient client, string? subject = null)
    {
        var response = await client.PostAsJsonAsync("/api/auth/token", new { Subject = subject });
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        return payload!.AccessToken;
    }

    internal record TokenResponseDto(string AccessToken, string TokenType, int ExpiresInSeconds);
}
