# Documento Arquitetural - Fluxo de Caixa

## Contexto

Um comerciante precisa registrar diariamente seus lançamentos financeiros (créditos e débitos) e acompanhar seu saldo consolidado diário. A arquitetura da solução deve garantir alta disponibilidade, segurança e resiliência, permitindo que o serviço de lançamentos permaneça funcional mesmo com falhas no serviço de consolidação.

## Requisitos

### Requisitos Funcionais

- Registrar lançamentos financeiros (créditos e débitos).
- Consultar relatório de saldo consolidado diário.
- Separação entre serviços de lançamentos e consolidação.
- Resiliência entre os serviços: lançamentos não dependem da disponibilidade do serviço de consolidação.

### Requisitos Não-Funcionais

- O serviço de consolidação deve suportar 50 requisições por segundo com no máximo 5% de perda.
- O serviço de lançamentos deve ter alta disponibilidade mesmo com falhas no consolidado.
- A solução deve possuir mecanismos de proteção contra ataques

## Solução Proposta

### Arquitetura

- Arquitetura SOA com serviços desacoplados para lançamentos e consolidação.
- Comunicação assíncrona entre serviços usando RabbitMQ.
- Proxy (YARP) centralizando autenticação, roteamento e proteção.

### Decisões Arquiteturais

A arquitetura escolhida combina **SOA (Service-Oriented Architecture)** com **orientação a eventos**, baseada nos seguintes fatores:

- **Independência total entre os serviços**, alinhada aos requisitos de disponibilidade, resiliência e escalabilidade.
- **Facilidade de escalabilidade individual**, permitindo o crescimento isolado dos serviços conforme demanda.
- **Garantia de confiabilidade dos dados via eventos**: mesmo que o serviço de consolidação esteja temporariamente indisponível, os eventos serão armazenados na fila e reprocessados posteriormente, sem perda de informações.
- **Microsserviços não foram adotados** neste estágio devido à falta de conhecimento aprofundado do domínio, o que dificultaria uma decomposição eficiente. Além disso, microserviços agregam uma complexidade operacional desnecessária no momento atual.

Embora a **arquitetura orientada a eventos** adicione certa complexidade, os benefícios em **disponibilidade, resiliência e desempenho** justificam essa escolha. Para mitigar os impactos dessa complexidade nos times de desenvolvimento, será criada uma **biblioteca de abstração** que encapsula os detalhes da infraestrutura de mensageria, permitindo que as equipes foquem nas regras de negócio.

Inicialmente, o **RabbitMQ** será adotado como broker de mensagens, por sua simplicidade e facilidade de operação. Caso a carga aumente substancialmente, será possível evoluir para uma solução mais robusta, como **Apache Kafka**, com **mínimo impacto na arquitetura**, graças ao baixo acoplamento entre os componentes.

A escolha do **YARP (Yet Another Reverse Proxy)** como gateway foi motivada por sua **integração nativa com o ecossistema .NET**, simplicidade de configuração e ausência, neste momento, de demandas que justifiquem o uso de soluções mais robustas como **Envoy** ou **Istio**. No entanto, a arquitetura foi desenhada de forma **extensível**, permitindo a substituição do YARP futuramente com baixo impacto técnico.

---

#### Persistência de Dados

- **Serviço de Lançamentos (transacional, relacional e consistente)**:  
  - **SQL Server** ou **PostgreSQL** são plenamente adequados.
  
- **Serviço de Consolidação (alta frequência de leitura, leitura intensiva e idempotência)**:  
  - **PostgreSQL** é preferível por oferecer melhor suporte a operações idempotentes (`INSERT ... ON CONFLICT`) e escalabilidade superior em leitura.

Para o **MVP**, é viável utilizar **SQL Server em ambos os serviços**, mantendo uma visão de evolução futura com a **migração do banco de consolidação para PostgreSQL**, caso surjam gargalos de leitura ou dificuldades com upserts eficientes.

---

#### Segurança

- Todos os dados sensíveis serão **criptografados em repouso**, e o tráfego será protegido com **TLS 1.2 ou superior**.
- As **APIs, a mensageria e os bancos de dados estarão isolados em redes privadas por ambiente** (desenvolvimento, homologação, produção), garantindo que ambientes diferentes não se comuniquem diretamente.
- Cada rede (API, mensageria e banco de dados) **permitirá acesso apenas às redes vizinhas com as quais interage diretamente**.  
  Exemplo: somente a rede da API de lançamentos financeiros terá acesso à rede do banco de dados correspondente.


### Como Atende aos Requisitos

- **Alta disponibilidade e resiliência:** separação clara entre serviços + fila de eventos.
- **Escalabilidade:** proxy e serviços podem ser escalados horizontalmente.
- **Segurança:** JWT + HTTPS + proxy único exposto + criptografia em repouso.
- **Performance:** uso de filas evita sobrecarga no consolidado e permite event processing assíncrono.
- **Observabilidade e Testes:** cobertura mínima, health checks e logs estruturados garantem robustez.