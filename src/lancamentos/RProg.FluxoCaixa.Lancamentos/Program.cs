using RProg.FluxoCaixa.Lancamentos.Application.Commands;
using RProg.FluxoCaixa.Lancamentos.Infrastructure.Data.Dapper;
using RProg.FluxoCaixa.Lancamentos.Infrastructure.Middleware;
using RabbitMQ.Client;
using System.Data;
using Microsoft.Data.SqlClient;
using RProg.FluxoCaixa.Lancamentos.Infrastructure;
using Serilog;
using System.Reflection;

// Configuração inicial do Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando aplicação Lançamentos");

    var builder = WebApplication.CreateBuilder(args);

    // Configuração do Serilog
    builder.Host.UseSerilog((context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    // Add services to the container.
    builder.Services.AddControllers();

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title = "RProg.FluxoCaixa.Consolidado API",
            Version = "v1",
            Description = "API para consulta de dados consolidados por período e categoria usando padrão CQRS"
        });

        // Incluir comentários XML
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
    builder.Services.AddScoped<IRegistrarLancamentoHandler, RegistrarLancamentoHandler>();
    builder.Services.AddScoped<ILancamentoRepository, LancamentoRepository>();

    builder.Services.AddScoped<IDbConnection>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var connectionString = config.GetConnectionString("DefaultConnection");

        return new SqlConnection(connectionString);
    });

    builder.Services.AddSingleton<IConnectionFactory>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        return new ConnectionFactory
        {
            HostName = config["RabbitMQ:HostName"] ?? string.Empty
        };
    });

    builder.Services.AddSingleton(async p =>
    {
        var connectionFactory = p.GetRequiredService<IConnectionFactory>();

        return await connectionFactory.CreateConnectionAsync();
    });

    // Configuração de Health Checks
    builder.Services.AddHealthChecks()
        .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database")
        .AddRabbitMQ(name: "rabbitmq");

    builder.Services.AddSingleton<IMensageriaPublisher, RabbitMqPublisher>();

    var app = builder.Build();

    app.UseHealthChecks("/api/health");

    // Middleware de tratamento de exceções
    app.UseMiddleware<TratarExcecoesMiddleware>();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "RProg.FluxoCaixa.Lancamentos API v1");
            c.RoutePrefix = string.Empty;
        });
    }

    app.UseHttpsRedirection();

    app.UseCors("AllowAll");

    app.UseAuthorization();

    app.MapControllers();

    Log.Information("Aplicação iniciada com sucesso");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Falha ao iniciar a aplicação");
}
finally
{
    Log.CloseAndFlush();
}
