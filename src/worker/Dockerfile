# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/worker/RProg.FluxoCaixa.Worker/RProg.FluxoCaixa.Worker.csproj", "worker/RProg.FluxoCaixa.Worker/"]
RUN dotnet restore "worker/RProg.FluxoCaixa.Worker/RProg.FluxoCaixa.Worker.csproj"

# Copy source code and build
COPY ["src/worker/RProg.FluxoCaixa.Worker/", "worker/RProg.FluxoCaixa.Worker/"]
WORKDIR "/src/worker/RProg.FluxoCaixa.Worker"
RUN dotnet build "RProg.FluxoCaixa.Worker.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "RProg.FluxoCaixa.Worker.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create logs directory
RUN mkdir -p /app/logs

# Copy published application
COPY --from=publish /app/publish .

# Create non-root user for security
RUN adduser --disabled-password --home /app --gecos '' --shell /bin/bash dotnetuser \
    && chown -R dotnetuser:dotnetuser /app
USER dotnetuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD dotnet --info || exit 1

# Set environment variables
ENV DOTNET_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=

# Entry point
ENTRYPOINT ["dotnet", "RProg.FluxoCaixa.Worker.dll"]
