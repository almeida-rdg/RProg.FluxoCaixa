FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RProg.FluxoCaixa.Lancamentos/RProg.FluxoCaixa.Lancamentos.csproj", "RProg.FluxoCaixa.Lancamentos/"]
RUN dotnet restore "./RProg.FluxoCaixa.Lancamentos/RProg.FluxoCaixa.Lancamentos.csproj"
COPY . .
WORKDIR "/src/RProg.FluxoCaixa.Lancamentos"
RUN dotnet build "./RProg.FluxoCaixa.Lancamentos.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RProg.FluxoCaixa.Lancamentos.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RProg.FluxoCaixa.Lancamentos.dll"]