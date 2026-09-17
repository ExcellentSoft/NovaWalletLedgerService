namespace NovaWallet.Application.Options;

/// <summary>
/// Configuration for wallet business rules. Bind from the "Wallet" section of appsettings.
/// </summary>
public class WalletOptions
{
    public const string SectionName = "Wallet";

    /// <summary>
    /// Server-side daily outbound transfer limit per wallet, in kobo (default ₦500,000/day = 50,000,000 kobo).
    /// Enforced regardless of any client-supplied limit, per CBN consumer-protection expectations.
    /// </summary>
    public long DailyOutboundTransferLimitKobo { get; set; } = 500_000_00;
}
