# Migrations

This folder holds Entity Framework Core migrations for the `NovaWalletDbContext` (PostgreSQL).

Generate the initial migration from the `NovaWallet.Infrastructure` directory with:

```powershell
dotnet ef migrations add InitialCreate --project ../NovaWallet.Infrastructure --startup-project ../NovaWallet.API --output-dir Persistence/Migrations
```

Apply migrations to the database with:

```powershell
dotnet ef database update --project ../NovaWallet.Infrastructure --startup-project ../NovaWallet.API
```
