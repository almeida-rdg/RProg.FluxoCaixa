using RProg.FluxoCaixa.Lancamentos.Application.Commands;
using RProg.FluxoCaixa.Lancamentos.Infrastructure.Data.Dapper;
using RProg.FluxoCaixa.Lancamentos.Infrastructure.Middleware;
using RabbitMQ.Client;
using System.Data;
using Microsoft.Data.SqlClient;
using RProg.FluxoCaixa.Lancamentos.Infrastructure;
using Serilog;

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
    builder.Services.AddSwaggerGen();    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RegistrarLancamentoHandler>());
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

    builder.Services.AddSingleton<IMensageriaPublisher, RabbitMqPublisher>();

    var app = builder.Build();

    // Middleware de tratamento de exceções
    app.UseMiddleware<TratarExcecoesMiddleware>();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

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
