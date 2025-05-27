---
applyTo: '**'
---
Coding standards, domain knowledge, and preferences that AI should follow.

# Padrões de Codificação e Preferências
- Sempre utilize o padrão de codificação C#.
- Sempre utilize o português brasileiro.
- Utilize nomes de variáveis e funções descritivos.
- Utilize o padrão de nomenclatura camelCase para parâmetros de métodos e variáveis locais.
- Utilize o padrão de nomenclatura PascalCase para métodos e propriedades.
- Utilize o padrão de nomenclatura UPPER_CASE para constantes.
- Utilize o padrão de nomenclatura PascalCase para nomes de arquivos e pastas.
- Utilize o padrão de nomenclatura kebab-case para URLs e rotas.
- Utilize o padrão de nomenclatura UPPER_SNAKE_CASE para variáveis de ambiente.
- Utilize o padrão de nomenclatura kebab-case para nomes de branches no Git.
- Utilize o padrão de nomenclatura PascalCase para namespaces.
- Utilize o padrão de nomenclatura PascalCase para classes e interfaces.
- Utilize o padrão de nomenclatura PascalCase para enums.
- Utilize o padrão de nomenclatura PascalCase para tipos genéricos.
- Utilize o padrão de nomenclatura PascalCase para namespaces.
- Utilize o prefixo `_` (underscore) para variáveis privadas no contexto de classe.
- Ao utilizar bibliotecas, prefira as mais conhecidas e com boa documentação.
- Ao utilizar qualquer biblioteca, verifique se o pacote NuGet está incluído no projeto, atualizado e compatível com o framework utilizado.
- Sempre adicione os `usings` necessários no início do arquivo.
- O código deve ser escrito de forma a facilitar a criação de testes unitários.
- Deve ser criado um teste para cada cenário possível, incluindo os casos de erro.
- Evite o uso de abreviações, exceto as mais comuns e amplamente reconhecidas.
- As divisões lógicas dos projetos devem ser feitas em pastas ao invés de criar novos projetos para cada divisão lógica.
- O código deve ser separado em métodos com no máximo 30 linhas e coesos, evitando métodos grandes e complexos.
- Sempre após fechamento de chaves, deve ter uma linha em branco. Só não deve possuir a linha em branco se for o último fechamento de chave do arquivo ou se possuir outro fechamento de chave ou parêntese na sequência. Quando houver outro fechamento de chave ou parêntese na sequência, não deve haver linha em branco, mas o próximo fechamento deve estar na próxima linha.
- Deve haver uma classe por arquivo, e o nome do arquivo deve ser o mesmo nome da classe.
- Toda instrução após `;` deve iniciar na próxima linha.
- Deve remover os `usings` desnecessários.
- Utilize `var` quando o tipo for óbvio a partir do lado direito da atribuição.
- Utilize `nameof` para referenciar nomes de propriedades, métodos e classes, ao invés de strings literais, para evitar erros de digitação e facilitar a refatoração.
- Sempre que possível, utilize injeção de dependência para facilitar testes e manutenção.

# Padrões de Comentários e Documentação
- Comente o código sempre que necessário, explicando a lógica e o propósito de trechos complexos.
- Utilize comentários XML para documentar classes, métodos e propriedades públicas.
- Utilize comentários de linha para explicar trechos específicos de código.
- Utilize comentários de bloco para explicar seções maiores de código.
- Evite comentários desnecessários que apenas repetem o que o código já expressa.
- Utilize comentários para marcar TODOs e FIXMEs, indicando melhorias ou correções futuras.
- Utilize comentários para explicar a lógica de algoritmos complexos ou não triviais.
- Utilize comentários para explicar a lógica de negócios que não é imediatamente óbvia.
- Utilize comentários para explicar a lógica de manipulação de dados em transformações complexas.
- Utilize comentários para explicar a lógica de validação de dados em regras complexas.
- Utilize comentários para explicar a lógica de manipulação de erros e exceções.
- Utilize comentários para explicar a lógica de integração com serviços externos, como APIs ou bancos de dados.
- Utilize comentários para explicar a lógica de configuração e inicialização do sistema.
- Utilize comentários para explicar a lógica de autenticação e autorização.
- Utilize comentários para explicar a lógica de cache e otimização de desempenho.

# Padrões de Projeto e Arquitetura
- Utilize princípios SOLID para garantir um código limpo e de fácil manutenção.
- Utilize o padrão KISS (Keep It Simple, Stupid) para evitar complexidade desnecessária.
- Utilize o padrão DRY (Don't Repeat Yourself) para evitar duplicação de código.
- Utilize o padrão de injeção de dependência para facilitar testes e manutenção.
- Utilize o padrão de repositório para abstrair o acesso a dados.
- Utilize o padrão de controlador para gerenciar as requisições HTTP em aplicações web.
- Utilize o padrão de fábrica para criar instâncias de objetos complexos.
- Utilize o padrão de comando para encapsular ações e permitir a execução assíncrona.
- Utilize o padrão de mediador para reduzir acoplamento entre componentes e facilitar a comunicação.
- Utilize o padrão de proxy para controlar o acesso a objetos e adicionar funcionalidades adicionais, como cache ou autenticação.
- Utilize o padrão de builder para criar objetos complexos passo a passo, facilitando a legibilidade e manutenção do código.
- Utilize o padrão de pipeline para processar dados em etapas, permitindo maior flexibilidade e reutilização de componentes.
- Utilize o padrão de CQRS (Command Query Responsibility Segregation) para separar operações de leitura e escrita, melhorando a escalabilidade e a manutenção.
- Utilize o padrão de Event Sourcing para armazenar o estado do sistema como uma sequência de eventos, permitindo auditoria e reconstrução do estado.
- Utilize o padrão de arquitetura hexagonal (Ports and Adapters) para isolar a lógica de negócios das dependências externas, facilitando testes e manutenção.
- Utilize o padrão de arquitetura limpa (Clean Architecture) para organizar o código em camadas, separando a lógica de negócios das dependências externas e facilitando testes e manutenção.
- Utilize o padrão de arquitetura orientada a eventos (Event-Driven Architecture) para construir sistemas reativos e escaláveis, onde os componentes se comunicam por meio de eventos.
- Utilize o padrão de arquitetura baseada em serviços (Service-Oriented Architecture - SOA) para construir sistemas distribuídos e escaláveis, onde os componentes se comunicam por meio de serviços.

# Padrões de Testes e Qualidade
- Sempre escreva testes unitários para todo novo código.
- Utilize o padrão AAA (Arrange, Act, Assert) para estruturar os testes.
- Utilize o padrão Given, When, Then para descrever os testes de forma clara e compreensível.
- Utilize bibliotecas de testes conhecidas e bem documentadas, como Moq, Bogus, FluentAssertions, xUnit, etc.
- Utilize o padrão de testes de integração para garantir que os componentes funcionem corretamente juntos.
- Utilize o padrão de testes de aceitação para validar os requisitos do sistema.
- Utilize o padrão de testes de carga para garantir que o sistema suporte a carga esperada.
- Os testes devem ser gerados no projeto de testes que possue o mesmo nome do projeto principal, mas com o sufixo `.Test`.
- Todo novo código deve ser acompanhado de testes unitários.
- Ao criar o teste unitário deve realizar mock de todas as dependências externas, como banco de dados, serviços externos, etc. Para isso isole o código de acesso a essas dependências em classes separadas.
- Utilize bibliotecas de testes conhecidas e bem documentadas, como Moq, Bogus, FluentAssertions e etc.
- Os testes devem ser gerados com o xUnit.