using MediatR;
using RProg.FluxoCaixa.Lancamentos.Domain.Entities;
using RProg.FluxoCaixa.Lancamentos.Domain.Exceptions;
using RProg.FluxoCaixa.Lancamentos.Infrastructure;
using RProg.FluxoCaixa.Lancamentos.Infrastructure.Data.Dapper;

namespace RProg.FluxoCaixa.Lancamentos.Application.Commands
{
    /// <summary>
    /// Handler responsável por processar comandos de registro de lançamentos financeiros.
    /// </summary>
    public class RegistrarLancamentoHandler : IRequestHandler<RegistrarLancamentoCommand, int>
    {
        private readonly IMensageriaPublisher _mensageriaPublisher;
        private readonly ILancamentoRepository _lancamentoRepository;
        private readonly ILogger<RegistrarLancamentoHandler> _logger;

        public RegistrarLancamentoHandler(
            IMensageriaPublisher mensageriaPublisher, 
            ILancamentoRepository lancamentoRepository, 
            ILogger<RegistrarLancamentoHandler> logger)
        {
            _mensageriaPublisher = mensageriaPublisher;
            _lancamentoRepository = lancamentoRepository;
            _logger = logger;
        }

        public async Task<int> Handle(RegistrarLancamentoCommand comando, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando registro de lançamento. Valor: {Valor}, Tipo: {Tipo}, Data: {Data}", 
                comando.Valor, comando.Tipo, comando.Data);

            try
            {
                ValidarParametrosOperacao(comando);

                var lancamento = new Lancamento(
                    comando.Valor,
                    comando.Tipo,
                    comando.Data,
                    comando.Categoria,
                    comando.Descricao);

                var id = await _lancamentoRepository.CriarLancamentoAsync(lancamento);
                _logger.LogInformation("Lançamento criado no banco de dados com ID: {LancamentoId}", id);

                await _mensageriaPublisher.PublicarMensagemAsync(lancamento with {
                    Id = id
                });
                _logger.LogInformation("Mensagem publicada na fila para lançamento ID: {LancamentoId}", id);

                _logger.LogInformation("Lançamento registrado com sucesso. ID: {LancamentoId}", id);

                return id;
            }
            catch (Exception ex) when (!(ex is ExcecaoNegocio))
            {
                _logger.LogError(ex, "Erro inesperado ao registrar lançamento. Valor: {Valor}, Tipo: {Tipo}", 
                    comando.Valor, comando.Tipo);
                throw;
            }
        }

        private void ValidarParametrosOperacao(RegistrarLancamentoCommand comando)
        {
            if (comando == null)
            {
                throw new ExcecaoDadosInvalidos("O comando não pode ser nulo.");
            }

            if (string.IsNullOrWhiteSpace(comando.Descricao))
            {
                throw new ExcecaoDadosInvalidos("A descrição é obrigatória.");
            }

            if (string.IsNullOrWhiteSpace(comando.Categoria))
            {
                throw new ExcecaoDadosInvalidos("A categoria é obrigatória.");
            }
        }
    }
}
