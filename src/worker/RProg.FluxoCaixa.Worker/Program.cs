using RProg.FluxoCaixa.Worker;
using RProg.FluxoCaixa.Worker.Infrastructure.Data;
using RProg.FluxoCaixa.Worker.Infrastructure.Services;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.AddScoped<RProg.FluxoCaixa.Worker.Domain.Services.IConsolidacaoService, RProg.FluxoCaixa.Worker.Services.ConsolidacaoService>();
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

builder.Services.AddScoped<IConsolidadoRepository, ConsolidadoRepository>();
builder.Services.AddScoped<ILancamentoProcessadoRepository, LancamentoProcessadoRepository>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

try
{
    Log.Information("Iniciando Worker de Consolidação RProg.FluxoCaixa");
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Falha fatal na aplicação");
}
finally
{
    Log.CloseAndFlush();
}
