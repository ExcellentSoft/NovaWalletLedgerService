using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaWallet.Application.Dtos
{
    
    /// <summary>Request body for crediting a wallet (WalletId comes from the route).</summary>
    public record CreditWalletBody(long AmountKobo, string? Reference = null);

    /// <summary>Request body for a transfer (IdempotencyKey comes from the Idempotency-Key header).</summary>
    public record TransferBody(Guid FromWalletId, Guid ToWalletId, long AmountKobo, string? Reference = null);
}
