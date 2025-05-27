using MediatR;

namespace RProg.FluxoCaixa.Lancamentos.Application.Commands
{
    public interface IRegistrarLancamentoHandler : IRequestHandler<RegistrarLancamentoCommand, int>
    {
        // Interface para o handler de registrar lançamentos
        // Define o método que será implementado para processar o comando RegistrarLancamentoCommand
    }
}
