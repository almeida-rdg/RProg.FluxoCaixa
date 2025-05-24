# RProg.FluxoCaixa

## Objetivo

A aplicação **RProg.FluxoCaixa** foi projetada para atender comerciantes que precisam registrar lançamentos financeiros diários (créditos e débitos) e consultar o saldo consolidado por dia. A arquitetura proposta garante alta disponibilidade, resiliência e segurança, separando os serviços de forma escalável e permitindo comunicação assíncrona entre os módulos.

O sistema foi construído com foco em boas práticas de engenharia de software e facilidade de evolução futura.

## Estrutura do Projeto

```text
/RProg.FluxoCaixa
|-- docs/
|   |-- c4contexto.png
|   |-- c4container.png
|   |-- documento-arquitetural.md
|-- src/
|   |-- proxy/
|   |   |-- RProg.FluxoCaixa.Proxy/
|   |   |-- RProg.FluxoCaixa.Proxy.Test/
|   |-- lancamentos/
|   |   |-- RProg.FluxoCaixa.Lancamentos/
|   |   |-- RProg.FluxoCaixa.Lancamentos.Test/
|   |-- consolidado/
|   |   |-- RProg.FluxoCaixa.Consolidado/
|   |   |-- RProg.FluxoCaixa.Consolidado.Test/
|   |-- worker/
|   |   |-- RProg.FluxoCaixa.Worker/
|   |   |-- RProg.FluxoCaixa.Worker.Test/
|   |-- docker-compose.yml
|   |-- README.md
|-- README.md
|-- RProg.FluxoCaixa.slnx
```

## Componentes

- `docs/`: Documentação arquitetural e diagramas C4.
- `src/`: Código-fonte e testes organizados por contexto de domínio.
- `docker-compose.yml`: Orquestração local de todos os serviços e infraestrutura.
- `RProg.FluxoCaixa.slnx`: Solution principal que unifica os projetos.

## Documentação

A arquitetura, requisitos e decisões técnicas estão descritos em:
- [`docs/documento-arquitetural.md`](docs/documento-arquitetural.md)