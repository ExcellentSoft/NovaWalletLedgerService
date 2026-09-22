namespace NovaWallet.Domain.Entities;

public class DailyTransferUsage
{
    public Guid Id { get; private set; }
    public Guid WalletId { get; private set; }
    public DateOnly UsageDate { get; private set; }
    public long TotalTransferred { get; private set; }
    //Id, WalletId, UsageDate, TotalTransferred
    private DailyTransferUsage()
    {
    }

    public DailyTransferUsage(Guid id, Guid walletId, DateOnly usageDate, long totalTransferred = 0)
    {
        Id = id;
        WalletId = walletId;
        UsageDate = usageDate;
        TotalTransferred = totalTransferred;
    }

    public void AddTransfer(long amount)
    {
        TotalTransferred += amount;
    }
}
