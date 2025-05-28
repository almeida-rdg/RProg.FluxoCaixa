using RProg.FluxoCaixa.Proxy.Authentication;
using RProg.FluxoCaixa.Proxy.Middleware;
using RProg.FluxoCaixa.Proxy.Services;
using Serilog;
using Yarp.ReverseProxy.Configuration;

// Configuração do Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Iniciando aplicação FluxoCaixa Proxy");

    var builder = WebApplication.CreateBuilder(args);

    // Configuração do Serilog
    builder.Host.UseSerilog();

    // Configuração do cache em memória
    builder.Services.AddMemoryCache(options =>
    {
        options.SizeLimit = builder.Configuration.GetValue<int>("Cache:SizeLimit", 1000);
    });    // Configuração de autenticação JWT
    builder.Services.AddJwtAuthentication(builder.Configuration);
    
    // Registrar serviços JWT
    builder.Services.AddScoped<JwtTokenService>();

    // Configuração de CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultPolicy", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Health checks
    builder.Services.AddHealthChecks()
        .AddCheck("proxy-health", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Proxy está funcionando"));    // Configuração do YARP
    builder.Services.AddSingleton<IProxyConfigProvider>(serviceProvider =>
    {
        var logger = serviceProvider.GetRequiredService<ILogger<ConfiguracaoYarpProvider>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        return new ConfiguracaoYarpProvider(logger, configuration);
    });
    
    builder.Services.AddSingleton<ConfiguracaoYarpProvider>(serviceProvider =>
    {
        var logger = serviceProvider.GetRequiredService<ILogger<ConfiguracaoYarpProvider>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        return new ConfiguracaoYarpProvider(logger, configuration);
    });

    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    // Serviços customizados
    builder.Services.AddHostedService<MonitoramentoContainersService>();

    // Swagger para documentação (apenas em desenvolvimento)
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() 
            { 
                Title = "FluxoCaixa Proxy API", 
                Version = "v1",
                Description = "Proxy reverso com balanceamento de carga, cache e proteção para as APIs do FluxoCaixa"
            });
        });
    }

    var app = builder.Build();

    // Pipeline de middlewares
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "FluxoCaixa Proxy API V1");
            c.RoutePrefix = "swagger";
        });
    }

    // Middleware de logging de requisições
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondido {StatusCode} em {Elapsed:0.0000} ms";
        options.GetLevel = (httpContext, elapsed, ex) => ex != null 
            ? Serilog.Events.LogEventLevel.Error 
            : elapsed > 1000 
                ? Serilog.Events.LogEventLevel.Warning 
                : Serilog.Events.LogEventLevel.Information;
    });    // Middlewares de segurança e proteção
    app.UseMiddleware<TokenExtractionMiddleware>();
    app.UseMiddleware<SecurityMiddleware>();
    app.UseMiddleware<RateLimitingMiddleware>();

    // Middleware de cache (antes do YARP)
    app.UseMiddleware<CacheMiddleware>();

    // CORS
    app.UseCors("DefaultPolicy");

    // Autenticação e autorização
    app.UseAuthentication();
    app.UseAuthorization();

    // Health checks
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready");
    app.MapHealthChecks("/health/live");

    // Endpoint de métricas básicas
    app.MapGet("/metrics", async (HttpContext context) =>
    {
        var metricas = new
        {
            timestamp = DateTime.UtcNow,
            uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime(),
            memoryUsage = GC.GetTotalMemory(false),
            processorCount = Environment.ProcessorCount,
            version = "1.0.0"
        };

        await context.Response.WriteAsJsonAsync(metricas);
    });

    // Endpoint de informações do proxy
    app.MapGet("/proxy/info", () =>
    {
        return Results.Json(new
        {
            name = "FluxoCaixa Proxy",
            version = "1.0.0",
            description = "Proxy reverso com balanceamento de carga, cache e proteção",
            features = new[]
            {
                "Load Balancing",
                "Health Checks", 
                "Circuit Breaker",
                "Rate Limiting",
                "Security Protection",
                "Response Caching",
                "Auto Scaling"
            },
            uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()
        });
    });

    // Configuração do YARP como último middleware
    app.MapReverseProxy();

    // Tratamento de erros globais
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            Log.Error("Erro não tratado na aplicação");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Erro interno do servidor");
        });
    });

    Log.Information("Aplicação configurada com sucesso. Iniciando servidor...");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação terminou inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
