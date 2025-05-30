using RProg.FluxoCaixa.Proxy.Middleware;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Iniciando aplicação FluxoCaixa Proxy");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultPolicy", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });
    builder.Services.AddOutputCache(options =>
    {
        options.AddBasePolicy(builder =>
        {
            builder.Expire(TimeSpan.FromMinutes(5));
        });
    });

    builder.Services.AddHealthChecks()
        .AddCheck("proxy-health", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Proxy está funcionando"));
    
    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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

    app.UseCors("DefaultPolicy");
    app.UseOutputCache();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "FluxoCaixa Proxy API V1");
            c.RoutePrefix = "";
        });
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondido {StatusCode} em {Elapsed:0.0000} ms";
        options.GetLevel = (httpContext, elapsed, ex) => ex != null 
            ? Serilog.Events.LogEventLevel.Error 
            : elapsed > 1000 
                ? Serilog.Events.LogEventLevel.Warning 
                : Serilog.Events.LogEventLevel.Information;
    });
    
    app.UseMiddleware<SecurityMiddleware>();
    app.UseMiddleware<RateLimitingMiddleware>();
    
    app.MapHealthChecks("/api/health");
    app.MapHealthChecks("/api/health/detailed", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds
                })
            };
            await context.Response.WriteAsJsonAsync(result);
        }
    });

    app.MapGet("/api/metrics", async (HttpContext context) =>
    {
        var metricas = new
        {
            timestamp = DateTime.UtcNow,
            uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime(),
            memoryUsage = GC.GetTotalMemory(false) / 1024 / 1024, // em MB
            cpuUsage = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount,
            processorCount = Environment.ProcessorCount,
            version = "1.0.0"
        };

        await context.Response.WriteAsJsonAsync(metricas, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    });

    app.MapGet("/api/proxy/info", () =>
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
                "Rate Limiting",
                "Security Protection",
                "Response Caching"
            },
            uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()
        }, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    });

    app.MapReverseProxy();

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
