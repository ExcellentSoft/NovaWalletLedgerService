FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY NovaWallet.Domain/NovaWallet.Domain.csproj NovaWallet.Domain/
COPY NovaWallet.Application/NovaWallet.Application.csproj NovaWallet.Application/
COPY NovaWallet.Infrastructure/NovaWallet.Infrastructure.csproj NovaWallet.Infrastructure/
COPY NovaWallet.API/NovaWallet.API.csproj NovaWallet.API/
RUN dotnet restore NovaWallet.API/NovaWallet.API.csproj

COPY NovaWallet.Domain/ NovaWallet.Domain/
COPY NovaWallet.Application/ NovaWallet.Application/
COPY NovaWallet.Infrastructure/ NovaWallet.Infrastructure/
COPY NovaWallet.API/ NovaWallet.API/

WORKDIR /src/NovaWallet.API
RUN dotnet publish NovaWallet.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "NovaWallet.API.dll"]
