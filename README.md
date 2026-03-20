# DataAccess Benchmark


Ao estudar performance, não estamos apenas medindo tempo de execução.
Estamos observando a **história da engenharia de software se manifestando em código**.

Este repositório foi criado para demonstrar, de forma prática, a evolução das abordagens de acesso a dados no ecossistema .NET, comparando:

* ADO.NET (baixo nível, controle total)
* Dapper (micro-ORM, performance com praticidade)
* Entity Framework Core (ORM completo, foco em produtividade)

Tudo medido com precisão usando **BenchmarkDotNet**, a ferramenta padrão para benchmarks profissionais em .NET.

Este projeto não é apenas um teste.
Ele é um laboratório para entender **como a engenharia de software evoluiu ao longo dos anos**.

---

# A História do Acesso a Dados no .NET

Assim como a POO surgiu da Crise do Software, as diferentes formas de acessar banco de dados surgiram de um problema recorrente:

> Como acessar dados com segurança, performance e manutenibilidade ao mesmo tempo?

Durante a evolução do .NET, três grandes abordagens se consolidaram.

---

## Linha do tempo da evolução

| Período      | Tecnologia                 | Filosofia                                 |
| ------------ | -------------------------- | ----------------------------------------- |
| 2002         | ADO.NET                    | Controle total, responsabilidade total    |
| 2011         | Dapper                     | Performance sem abrir mão da simplicidade |
| 2008 → atual | Entity Framework / EF Core | Produtividade e modelagem de domínio      |
| Atualidade   | BenchmarkDotNet            | Medir antes de opinar                     |

Cada uma resolve um problema diferente.

Este projeto mostra isso na prática.

---

# O Problema Real

Todo sistema precisa fazer operações básicas:

* Inserir dados
* Ler um registro
* Ler listas grandes
* Mapear dados para objetos

A pergunta não é:

> Qual tecnologia é melhor?

A pergunta correta é:

> Qual tecnologia é melhor para cada cenário?

Por isso este benchmark executa:

* Inserção de 10.000 registros
* Consulta por Id
* Consulta de lista com 10.000 registros
* Medição de tempo e memória

---

# O Papel do BenchmarkDotNet

Benchmarks feitos com `Stopwatch` não são confiáveis.

Problemas comuns:

* JIT interfere
* GC interfere
* CPU turbo interfere
* Warmup não controlado
* Otimizações do compilador

O **BenchmarkDotNet** resolve isso.

Ele:

* Faz warmup
* Executa múltiplas iterações
* Mede alocação de memória
* Mede tempo real
* Isola efeitos do runtime

Por isso ele é usado por:

* Microsoft
* Runtime do .NET
* Bibliotecas oficiais
* Engenheiros de performance

Neste projeto usamos BenchmarkDotNet como um engenheiro usaria.

---

# As três abordagens testadas

## 1. ADO.NET — O controle absoluto

ADO.NET é a forma mais antiga e mais direta de acessar o banco.

Você escreve SQL.
Você controla conexão.
Você controla leitura.
Você controla tudo.

Vantagens:

* Máxima performance
* Zero abstração
* Controle total

Desvantagens:

* Muito código
* Alto risco de erro
* Difícil manutenção

Usado em:

* Sistemas críticos
* ETL
* Engines de dados
* Frameworks internos

---

## 2. Dapper — O equilíbrio

Dapper é um micro-ORM criado pela equipe do StackOverflow.

Ele mantém SQL manual, mas remove o código repetitivo.

Características:

* Muito rápido
* Pouca abstração
* Mapeamento automático
* Sem tracking

Ele é famoso por ser:

> Quase tão rápido quanto ADO.NET, mas muito mais simples.

Usado em:

* APIs de alta performance
* Sistemas financeiros
* Jogos
* Microservices

---

## 3. Entity Framework Core — A modelagem

EF Core não é só acesso a dados.

Ele é uma ferramenta de modelagem de domínio.

Você trabalha com objetos, não com SQL.

Características:

* Change Tracking
* LINQ
* Migrations
* DbContext
* Relacionamentos
* Lazy / Eager loading

Vantagens:

* Alta produtividade
* Código limpo
* Fácil manutenção

Desvantagens:

* Overhead
* Mais memória
* Mais CPU

Por isso usamos:

```
AsNoTracking()
```

Para medir o EF sem o custo de rastreamento.

Isso mostra a diferença entre:

* EF completo
* EF otimizado
* Dapper
* ADO.NET

---

# O Cenário do Benchmark

Tabela simples:

```
Cliente
--------
Id
Nome
Email
Ativo
```

Operações testadas:

| Operação      | Objetivo                |
| ------------- | ----------------------- |
| Insert 10.000 | Testar escrita em massa |
| Select por Id | Testar acesso simples   |
| Select lista  | Testar materialização   |
| Mapping       | Testar custo de objetos |

Banco usado:

SQLite

Motivo:

* Fácil reproduzir
* Sem instalar SQL Server
* Mesmo comportamento para benchmark

---

# Estrutura do projeto

```
BenchmarkProject
 ├── Program.cs
 ├── Models
 │    └── Cliente.cs
 ├── Data
 │    ├── EfContext.cs
 │    └── DbFactory.cs
 └── Benchmarks
      └── ClienteBenchmark.cs
```

Separação feita para simular um projeto real.

Não é apenas um arquivo.

É engenharia.

---

# Como executar

Modo Release é obrigatório.

```
dotnet run -c Release
```

BenchmarkDotNet não funciona corretamente em Debug.

Ele precisa de otimizações ativadas.

---

# O que você deve observar nos resultados

Normalmente:

| Operação     | Mais rápido                    |
| ------------ | ------------------------------ |
| Insert       | Dapper / ADO                   |
| Select 1     | ADO                            |
| Select lista | Dapper                         |
| EF Core      | mais lento, mas mais produtivo |

Mas o objetivo não é ganhar.

O objetivo é entender o custo da abstração.

---

# A lição mais importante

Na engenharia profissional:

Não existe tecnologia perfeita.

Existe tecnologia adequada.

ADO.NET → controle
Dapper → performance + simplicidade
EF Core → produtividade + domínio

Um arquiteto precisa conhecer as três.

---

# Como estudar usando este projeto

1. Rode o benchmark
2. Leia o código de cada abordagem
3. Compare linha por linha
4. Veja onde está o custo
5. Veja onde está a abstração
6. Veja onde está a produtividade

Depois disso você começa a pensar como arquiteto.

---

# Engenharia pragmática

Este projeto segue um princípio simples:

* Não demonizar EF
* Não idolatrar Dapper
* Não romantizar ADO.NET

Cada ferramenta existe por um motivo.

O papel do engenheiro é entender o motivo.

---

# Próximos estudos recomendados

* Bulk Insert EF Core
* Compiled Queries EF Core
* Connection Pooling
* Change Tracking interno do EF
* Span / Memory no DataReader
* Pipeline do Dapper
* Query Plan SQL

Quando você entende isso, você sai do nível júnior.

---

> "Performance não é sobre escrever código rápido.
> É sobre entender o custo de cada decisão."
