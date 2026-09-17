using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace NovaWallet.IntegrationTests;

/// <summary>
/// Exercises the wallet API under concurrent load to verify the pessimistic-locking/idempotency
/// guarantees implemented in <c>UnitOfWork</c> and <c>WalletService</c> actually hold up against a
/// real Postgres instance (not just single-threaded unit tests).
/// </summary>
public class ConcurrencyTests : IClassFixture<NovaWalletApiFactory>
{
    private readonly NovaWalletApiFactory _factory;

    public ConcurrencyTests(NovaWalletApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await AuthenticationTests.GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<Guid> CreateWalletAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/wallets", new { CustomerId = Guid.NewGuid(), Currency = "NGN" });
        response.EnsureSuccessStatusCode();
        var wallet = await response.Content.ReadFromJsonAsync<WalletDto>();
        return wallet!.Id;
    }

    private static async Task CreditAsync(HttpClient client, Guid walletId, long amountKobo)
    {
        var response = await client.PostAsJsonAsync($"/api/wallets/{walletId}/credit", new { AmountKobo = amountKobo });
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Concurrency edge case: many transfers fire in parallel against a wallet with just enough
    /// balance for a single one of them to succeed. Row-level locking in the service must guarantee
    /// exactly one transfer succeeds and the wallet balance never goes negative.
    /// </summary>
    [Fact]
    public async Task Concurrent_Transfers_From_Same_Wallet_Never_Overdraw_The_Balance()
    {
        var client = await CreateAuthorizedClientAsync();

        var fromWalletId = await CreateWalletAsync(client);
        var toWalletId = await CreateWalletAsync(client);

        const long amountKobo = 10_000;
        const int concurrentRequests = 20;

        // Fund the source wallet so exactly one transfer of `amountKobo` can succeed.
        await CreditAsync(client, fromWalletId, amountKobo);

        var tasks = Enumerable.Range(0, concurrentRequests).Select(i => TransferAsync(fromWalletId, toWalletId, amountKobo, $"key-{i}-{Guid.NewGuid()}"));
        var results = await Task.WhenAll(tasks);

        var successCount = results.Count(r => r == HttpStatusCode.OK);
        Assert.Equal(1, successCount);

        var balanceResponse = await client.GetAsync($"/api/wallets/{fromWalletId}/balance");
        balanceResponse.EnsureSuccessStatusCode();
        var balance = await balanceResponse.Content.ReadFromJsonAsync<WalletBalanceDto>();

        Assert.Equal(0, balance!.BalanceKobo);
    }

    /// <summary>
    /// Concurrency edge case: the same Idempotency-Key is replayed concurrently (e.g., a client retry
    /// racing the original request). Exactly one attempt must actually move funds; all others must
    /// return either the cached success response or a conflict, never a second successful transfer.
    /// </summary>
    [Fact]
    public async Task Concurrent_Requests_With_Same_Idempotency_Key_Only_Transfer_Funds_Once()
    {
        var client = await CreateAuthorizedClientAsync();

        var fromWalletId = await CreateWalletAsync(client);
        var toWalletId = await CreateWalletAsync(client);

        const long amountKobo = 5_000;
        const int concurrentRequests = 15;

        await CreditAsync(client, fromWalletId, amountKobo * 5);

        var idempotencyKey = $"shared-key-{Guid.NewGuid()}";
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => TransferAsync(fromWalletId, toWalletId, amountKobo, idempotencyKey));
        var results = await Task.WhenAll(tasks);

        // Every response must be a "safe" outcome: either the successful/cached transfer, or a
        // conflict signalling another in-flight/duplicate attempt. None may silently double-transfer.
        Assert.All(results, status => Assert.True(status is HttpStatusCode.OK or HttpStatusCode.Conflict));
        Assert.Contains(results, r => r == HttpStatusCode.OK);

        var toBalanceResponse = await client.GetAsync($"/api/wallets/{toWalletId}/balance");
        toBalanceResponse.EnsureSuccessStatusCode();
        var toBalance = await toBalanceResponse.Content.ReadFromJsonAsync<WalletBalanceDto>();

        // Regardless of how many replays raced, the destination wallet must have received the
        // transfer amount exactly once.
        Assert.Equal(amountKobo, toBalance!.BalanceKobo);
    }

    private async Task<HttpStatusCode> TransferAsync(Guid fromWalletId, Guid toWalletId, long amountKobo, string idempotencyKey)
    {
        // Each concurrent caller uses its own HttpClient/authorized token to better emulate independent clients.
        var client = await CreateAuthorizedClientAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/wallets/transfer")
        {
            Content = JsonContent.Create(new { FromWalletId = fromWalletId, ToWalletId = toWalletId, AmountKobo = amountKobo })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private record WalletDto(Guid Id, Guid OwnerId, long BalanceKobo, string Currency, bool IsActive, DateTime CreatedAtUtc);

    private record WalletBalanceDto(Guid WalletId, long BalanceKobo, decimal BalanceNaira, string Currency);
}
