namespace RProg.FluxoCaixa.Lancamentos.Domain.Exceptions
{
    /// <summary>
    /// Exceção base para erros de negócio que devem retornar status 4xx.
    /// </summary>
    public abstract class ExcecaoNegocio : Exception
    {
        protected ExcecaoNegocio(string mensagem) : base(mensagem)
        {
        }

        protected ExcecaoNegocio(string mensagem, Exception innerException) : base(mensagem, innerException)
        {
        }
    }

    /// <summary>
    /// Exceção para violação de regras de negócio.
    /// </summary>
    public class ExcecaoRegraDeNegocio : ExcecaoNegocio
    {
        public ExcecaoRegraDeNegocio(string mensagem) : base(mensagem)
        {
        }

        public ExcecaoRegraDeNegocio(string mensagem, Exception innerException) : base(mensagem, innerException)
        {
        }
    }

    /// <summary>
    /// Exceção para dados de entrada inválidos.
    /// </summary>
    public class ExcecaoDadosInvalidos : ExcecaoNegocio
    {
        public ExcecaoDadosInvalidos(string mensagem) : base(mensagem)
        {
        }

        public ExcecaoDadosInvalidos(string mensagem, Exception innerException) : base(mensagem, innerException)
        {
        }
    }

    /// <summary>
    /// Exceção para recursos não encontrados.
    /// </summary>
    public class ExcecaoRecursoNaoEncontrado : ExcecaoNegocio
    {
        public ExcecaoRecursoNaoEncontrado(string mensagem) : base(mensagem)
        {
        }

        public ExcecaoRecursoNaoEncontrado(string mensagem, Exception innerException) : base(mensagem, innerException)
        {
        }
    }
}
