namespace NovaWallet.Domain.Entities;

public class DailyTransferUsage
{
    public Guid Id { get; private set; }
    public Guid WalletId { get; private set; }
    public DateOnly UsageDate { get; private set; }
    public decimal TotalTransferred { get; private set; }

    private DailyTransferUsage()
    {
    }

    public DailyTransferUsage(Guid id, Guid walletId, DateOnly usageDate, decimal totalTransferred = 0m)
    {
        Id = id;
        WalletId = walletId;
        UsageDate = usageDate;
        TotalTransferred = totalTransferred;
    }

    public void AddTransfer(decimal amount)
    {
        TotalTransferred += amount;
    }
}
