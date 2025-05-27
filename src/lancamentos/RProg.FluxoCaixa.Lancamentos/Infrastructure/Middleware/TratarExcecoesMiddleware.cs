using System.Net;
using System.Text.Json;
using RProg.FluxoCaixa.Lancamentos.Domain.Exceptions;

namespace RProg.FluxoCaixa.Lancamentos.Infrastructure.Middleware
{
    /// <summary>
    /// Middleware para tratamento global de exceções com diferenciação entre erros de negócio (4xx) e infraestrutura (5xx).
    /// </summary>
    public class TratarExcecoesMiddleware
    {
        private readonly RequestDelegate _proximoRequest;
        private readonly ILogger<TratarExcecoesMiddleware> _logger;

        public TratarExcecoesMiddleware(RequestDelegate proximoRequest, ILogger<TratarExcecoesMiddleware> logger)
        {
            _proximoRequest = proximoRequest;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _proximoRequest(context);
            }
            catch (Exception excecao)
            {
                await TratarExcecaoAsync(context, excecao);
            }
        }

        private async Task TratarExcecaoAsync(HttpContext context, Exception excecao)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var resposta = new RespostaErro();

            switch (excecao)
            {
                case ExcecaoDadosInvalidos ex:
                    _logger.LogWarning(ex, "Dados inválidos fornecidos: {Mensagem}", ex.Message);
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    resposta.Mensagem = ex.Message;
                    resposta.Tipo = "DadosInvalidos";
                    break;

                case ExcecaoRecursoNaoEncontrado ex:
                    _logger.LogWarning(ex, "Recurso não encontrado: {Mensagem}", ex.Message);
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    resposta.Mensagem = ex.Message;
                    resposta.Tipo = "RecursoNaoEncontrado";
                    break;

                case ExcecaoRegraDeNegocio ex:
                    _logger.LogWarning(ex, "Violação de regra de negócio: {Mensagem}", ex.Message);
                    response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                    resposta.Mensagem = ex.Message;
                    resposta.Tipo = "RegraDeNegocio";
                    break;

                case ExcecaoNegocio ex:
                    _logger.LogWarning(ex, "Erro de negócio: {Mensagem}", ex.Message);
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    resposta.Mensagem = ex.Message;
                    resposta.Tipo = "ErroNegocio";
                    break;

                default:
                    _logger.LogError(excecao, "Erro interno do servidor: {Mensagem}", excecao.Message);
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    resposta.Mensagem = "Ocorreu um erro interno no servidor. Tente novamente em alguns minutos.";
                    resposta.Tipo = "ErroInterno";
                    break;
            }

            resposta.StatusCode = response.StatusCode;
            resposta.TraceId = context.TraceIdentifier;

            var json = JsonSerializer.Serialize(resposta, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await response.WriteAsync(json);
        }
    }

    /// <summary>
    /// Modelo de resposta para erros.
    /// </summary>
    public class RespostaErro
    {
        public string Mensagem { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string TraceId { get; set; } = string.Empty;
    }
}
