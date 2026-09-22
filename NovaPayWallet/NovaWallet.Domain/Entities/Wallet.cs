using NovaWallet.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace NovaWallet.Domain.Entities;

public class Wallet
{
    // 
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public long Balance { get; private set; }
    public string Currency { get; private set; }
    
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public bool IsActive { get; private set; } 

    private Wallet()
    {
        Currency = string.Empty;
    }
    public Wallet(
      Guid customerId,
      string currency = "NGN")
    {
        Id = Guid.NewGuid();
        OwnerId = customerId;
        Currency = currency;
        Balance = 0;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }


    public void Credit(long amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("Credit amount must be positive.");
        }

        Balance += amount;
        UpdatedAtUtc = DateTime.UtcNow;
    }
   
    public void Debit(long amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("Debit amount must be positive.");
        }

        if (Balance < amount)
        {
            throw new InsufficientFundsException(Id, Balance, amount);
        }

        Balance -= amount;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
