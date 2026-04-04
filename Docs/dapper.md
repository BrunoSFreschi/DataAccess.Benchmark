Está uma versão pouco completa, estruturada como um material de estudo (quase uma mini apostila), com foco prático e performance.

---

# Dapper — Guia Prático

## 1. Visão Geral

O **Dapper** é um micro ORM (Object-Relational Mapper) para .NET que estende o ADO.NET. Ele foi projetado para oferecer:

- Alta performance (próximo ao ADO.NET puro)
- Baixo overhead
- Controle total sobre SQL
- Mapeamento simples entre objetos e queries

Diferente de ORMs completos como Entity Framework, o Dapper **não abstrai o SQL**, ele trabalha junto com ele.

### Quando usar Dapper

Use Dapper quando:

- Performance é crítica (ex: ETL, processamento em lote)
- Queries são complexas ou altamente otimizadas
- Você quer controle total do SQL
- Baixo overhead é necessário

Evite quando:

- Precisa de tracking automático de entidades
- Quer abstração completa do banco
- CRUD simples sem preocupação com performance

---

## 2. Instalação

Via CLI:

```bash
dotnet add package Dapper
```

Ou via NuGet Package Manager:

```bash
Install-Package Dapper
```

---

## 3. Conceitos Fundamentais

### 3.1 Extensão do IDbConnection

O Dapper funciona como extensão de `IDbConnection`:

```csharp
using (var connection = new SqlConnection(connectionString))
{
    var result = connection.Query("SELECT * FROM Clientes");
}
```

---

### 3.2 Query (SELECT)

Retorna dados mapeados automaticamente:

```csharp
var clientes = connection.Query<Cliente>(
    "SELECT Id, Nome, Email FROM Clientes"
);
```

Mapeia por convenção:

- Nome da coluna = Nome da propriedade

---

### 3.3 QueryFirst / QuerySingle

Diferença importante:

```csharp
// Retorna o primeiro ou default
var cliente = connection.QueryFirstOrDefault<Cliente>(
    "SELECT * FROM Clientes WHERE Id = @Id",
    new { Id = 1 }
);

// Espera exatamente 1 resultado
var cliente = connection.QuerySingle<Cliente>(
    "SELECT * FROM Clientes WHERE Id = @Id",
    new { Id = 1 }
);
```

Use `QuerySingle` quando:

- Você garante que existe exatamente 1 registro

---

### 3.4 Execute (INSERT, UPDATE, DELETE)

```csharp
var rows = connection.Execute(
    "INSERT INTO Clientes (Nome, Email) VALUES (@Nome, @Email)",
    new { Nome = "Gabrielly", Email = "email@email.com" }
);
```

Retorna:

- Número de linhas afetadas

---

## 4. Parâmetros e Segurança

### 4.1 Evitando SQL Injection

Sempre use parâmetros:

```csharp
var cliente = connection.Query<Cliente>(
    "SELECT * FROM Clientes WHERE Email = @Email",
    new { Email = email }
);
```

Nunca faça isso:

```csharp
// ERRADO
$"SELECT * FROM Clientes WHERE Email = '{email}'"
```

---

### 4.2 Parâmetros Dinâmicos

```csharp
var parameters = new DynamicParameters();
parameters.Add("Id", 1);

var cliente = connection.Query<Cliente>(
    "SELECT * FROM Clientes WHERE Id = @Id",
    parameters
);
```

---

## 5. Mapeamento Avançado

### 5.1 Multi-mapping (JOIN)

```csharp
var sql = @"
SELECT c.*, p.*
FROM Clientes c
INNER JOIN Pedidos p ON c.Id = p.ClienteId
";

var result = connection.Query<Cliente, Pedido, Cliente>(
    sql,
    (cliente, pedido) =>
    {
        cliente.Pedidos.Add(pedido);
        return cliente;
    }
);
```

---

### 5.2 Query Multiple

Executa múltiplas queries em uma chamada:

```csharp
using var multi = connection.QueryMultiple(@"
    SELECT * FROM Clientes;
    SELECT * FROM Pedidos;
");

var clientes = multi.Read<Cliente>();
var pedidos = multi.Read<Pedido>();
```

---

## 6. Transações

```csharp
using var connection = new SqlConnection(connectionString);
connection.Open();

using var transaction = connection.BeginTransaction();

try
{
    connection.Execute(sql1, param1, transaction);
    connection.Execute(sql2, param2, transaction);

    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

Essencial para:

- ETL
- Processamento em lote
- Consistência de dados

---

## 7. Performance

### 7.1 Por que o Dapper é rápido?

- Usa IL emit (geração dinâmica de código)
- Não faz tracking de entidades
- Não possui abstrações pesadas
- Trabalha direto sobre ADO.NET

### 7.2 Boas práticas de performance

- Reutilize conexões (connection pooling já ajuda)
- Use `QueryBuffered: false` para grandes volumes:

```csharp
var data = connection.Query<Cliente>(
    sql,
    buffered: false
);
```

- Evite SELECT *
- Use índices no banco
- Prefira queries específicas

---

## 8. Uso em ETL (seu cenário)

Dapper é excelente para ETL por:

- Baixo overhead
- Controle total do fluxo
- Alta velocidade em leitura/escrita

### Exemplo simplificado ETL:

```csharp
var dados = connectionOrigem.Query<Cliente>("SELECT * FROM Clientes");

foreach (var cliente in dados)
{
    connectionDestino.Execute(
        "INSERT INTO Clientes (Id, Nome) VALUES (@Id, @Nome)",
        cliente
    );
}
```

### Otimização recomendada:

- Processamento em lote
- Uso de transações
- Bulk insert quando possível

---

## 9. Comparação Técnica

| Característica | Dapper | ADO.NET | Entity Framework |
| --- | --- | --- | --- |
| Performance | Alta | Muito Alta | Média |
| Controle SQL | Total | Total | Parcial |
| Facilidade | Média | Baixa | Alta |
| Tracking | Não | Não | Sim |
| Overhead | Baixo | Muito baixo | Alto |

---

## 10. Boas Práticas

- Sempre use parâmetros
- Evite lógica no banco desnecessária
- Separe queries por responsabilidade
- Use DTOs específicos
- Não misture Dapper com lógica de domínio diretamente
- Centralize acesso a dados (Repository ou similar)

---

## 11. Armadilhas Comuns

- Esquecer de abrir conexão
- Não tratar múltiplos resultados corretamente
- Uso incorreto de `QuerySingle`
- Falta de transação em operações críticas
- Carregar grandes volumes em memória sem controle

---

## 12. Próximos Passos

Para evoluir no uso do Dapper:

1. Implementar um repositório genérico
2. Criar camada de acesso a dados desacoplada
3. Integrar com Minimal API (.NET)
4. Aplicar em um pipeline de ETL real
5. Medir performance (Benchmark)

---

## Referências e Documentação Oficial

Para aprofundar os estudos e entender as especificidades deste micro-ORM, utilize as fontes oficiais e os principais recursos da comunidade:

### 1. Documentação Principal
* **Repositório Oficial do Dapper (GitHub):** A principal fonte de verdade, contendo o código-fonte, exemplos de uso e a lista de métodos estendidos para a interface `IDbConnection`.
* **Dapper Tutorial:** Um portal mantido pela comunidade (e referenciado pelos criadores) que serve como um guia prático passo a passo para iniciantes e usuários avançados.
* **Documentação System.Data (Microsoft Learn):** Como o Dapper é construído sobre o ADO.NET, entender a interface `IDbConnection` e o ecossistema de conexões do .NET é fundamental.

### 2. Artigos de Comparação Técnica
* **Performance Benchmark Oficial:** A famosa tabela de desempenho localizada no README do repositório do Dapper, que compara o tempo de execução e alocação de memória entre Dapper, Entity Framework e ADO.NET puro.
* **Micro-ORMs vs ORMs Completos (Stack Overflow):** Discussões arquiteturais ricas sobre quando faz sentido abrir mão dos recursos complexos do Entity Framework em prol da velocidade e controle do Dapper.

### 3. Melhores Práticas
* **Mapeamento de Objetos e Relacionamentos (Multi-Mapping):** Guias sobre como utilizar recursos como `QueryAsync<T1, T2, TReturn>` para mapear consultas complexas com joins para objetos filhos.
* **Segurança e Consultas Parametrizadas:** Como o Dapper lida automaticamente com a parametrização de objetos anônimos para evitar ataques de SQL Injection de forma simples e elegante.

---

     
