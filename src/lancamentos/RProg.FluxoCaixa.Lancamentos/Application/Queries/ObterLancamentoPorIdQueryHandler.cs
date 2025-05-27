using MediatR;
using Microsoft.Extensions.Logging;
using RProg.FluxoCaixa.Lancamentos.Application.Queries;
using RProg.FluxoCaixa.Lancamentos.Domain.Entities;
using RProg.FluxoCaixa.Lancamentos.Infrastructure.Data.Dapper;

namespace RProg.FluxoCaixa.Lancamentos.Application.Queries
{
    /// <summary>
    /// Handler para a query de obtenção de lançamento por ID.
    /// </summary>
    public class ObterLancamentoPorIdQueryHandler : IRequestHandler<ObterLancamentoPorIdQuery, Lancamento?>
    {
        private readonly ILancamentoRepository _repositorio;
        private readonly ILogger<ObterLancamentoPorIdQueryHandler> _logger;

        public ObterLancamentoPorIdQueryHandler(ILancamentoRepository repositorio, ILogger<ObterLancamentoPorIdQueryHandler> logger)
        {
            _repositorio = repositorio;
            _logger = logger;
        }

        public async Task<Lancamento?> Handle(ObterLancamentoPorIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando busca de lançamento por ID {Id}", request.Id);

            var lancamento = await _repositorio.ObterPorIdAsync(request.Id, cancellationToken);

            if (lancamento == null)
            {
                _logger.LogWarning("Lançamento com ID {Id} não encontrado", request.Id);
            }
            else
            {
                _logger.LogInformation("Lançamento com ID {Id} encontrado com sucesso", request.Id);
            }

            return lancamento;
        }
    }
}
