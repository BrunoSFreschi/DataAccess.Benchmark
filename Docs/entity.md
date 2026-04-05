O Entity Framework (EF Core) fecha essa trindade do acesso a dados no .NET. Enquanto o ADO.NET te dá o controle mais baixo e o Dapper fica no meio termo, o EF Core é o peso-pesado da produtividade e da abstração.

Aqui está o guia, estruturado.

---

## Entity Framework Core — Guia Prático

### 1. Visão Geral

O Entity Framework Core (EF Core) é um mapeador objeto-relacional (ORM) moderno, de código aberto e multiplataforma para o .NET. Ele abstrai quase que totalmente o banco de dados, permitindo que você trabalhe com dados usando objetos C# fortemente tipados.

Ele foi projetado para oferecer:

- **Altíssima produtividade** (gera o SQL para você)
- **LINQ (Language Integrated Query)** para escrever queries direto no C#
- **Migrations** para controle de versão do esquema do banco de dados
- **Rastreamento de mudanças (*Change Tracking*)** automático

Diferente do Dapper e do ADO.NET, o EF Core foca em produtividade e em manter a lógica de dados próxima ao paradigma de orientação a objetos.

### Quando usar EF Core

Use EF Core quando:

- O foco principal do projeto é a velocidade de desenvolvimento (Time-to-Market)
- Você precisa de um CRUD padrão sem querer escrever SQL manualmente
- Deseja manter o código independente de banco de dados (fácil portabilidade de SQL Server para PostgreSQL, por exemplo)
- Precisa de recursos complexos como carregamento tardio (*Lazy Loading*) ou carregamento explícito

### Evite quando:

- Performance extrema de microssegundos for o requisito número um
- Você precisa executar queries altamente complexas ou muito específicas do banco
- Operações em lote (ETL) gigantescas sem o uso de recursos de Bulk específicos
- O overhead de memória do rastreamento de entidades for um problema

---

### 2. Instalação

O EF Core é modular. Você instala o pacote principal e o provedor do banco de dados escolhido.

Via CLI:

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
```

*(O pacote `Design` é necessário para rodar as Migrations).*

---

### 3. Conceitos Fundamentais

### 3.1 O DbContext

O `DbContext` é o coração do EF Core. Ele representa uma sessão com o banco de dados.

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Cliente> Clientes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("SuaConnectionStringAqui");
    }
}
```

### 3.2 Query (SELECT) com LINQ

Você não escreve SQL. Você usa LINQ, e o EF Core traduz para SQL em tempo de execução:

```csharp
using var context = new AppDbContext();

// Retorna todos os clientes com e-mail do Gmail
var clientes = context.Clientes
    .Where(c => c.Email.EndsWith("@gmail.com"))
    .ToList();
```

### 3.3 FirstOrDefault / SingleOrDefault

Funcionam de forma idêntica ao Dapper, mas usando LINQ:

```csharp
// Retorna o primeiro ou nulo
var cliente = context.Clientes.FirstOrDefault(c => c.Id == 1);

// Espera exatamente 1 resultado (lança exceção se houver mais de um)
var clienteUnico = context.Clientes.SingleOrDefault(c => c.Email == "email@email.com");
```

### 3.4 Operações de Escrita (INSERT, UPDATE, DELETE)

O EF Core rastreia as entidades em memória e só envia os comandos para o banco quando você chama `SaveChanges()`.

**Insert:**

```csharp
var novoCliente = new Cliente { Nome = "Gabrielly", Email = "email@email.com" };
context.Clientes.Add(novoCliente);
context.SaveChanges(); // Aqui o SQL INSERT é executado
```

**Update:**

```csharp
var cliente = context.Clientes.Find(1);
cliente.Nome = "Nome Alterado"; 
context.SaveChanges(); // O EF detecta a mudança e gera o UPDATE
```

---

### 4. Parâmetros e Segurança

### 4.1 SQL Injection? O EF Core já te protege

Como o LINQ gera consultas parametrizadas por padrão, você está automaticamente protegido contra SQL Injection na esmagadora maioria dos casos.

```csharp
string emailBusca = "usuario@email.com";
// Seguro por padrão. O EF cria o parâmetro @p0 no SQL gerado.
var cliente = context.Clientes.FirstOrDefault(c => c.Email == emailBusca);
```

### 4.2 Executando SQL Puro com Segurança

Se precisar rodar SQL manual no EF, use `FromSql` com interpolação segura:

```csharp
// O EF Core converte a interpolação em parâmetros de forma segura!
var clientes = context.Clientes
    .FromSql($"SELECT * FROM Clientes WHERE Nome = {nomeVar}")
    .ToList();
```

---

### 5. Mapeamento Avançado

### 5.1 Carregamento de Relacionamentos (JOIN)

Para trazer dados de tabelas relacionadas, você usa o `Include` (*Eager Loading*):

```csharp
var clientesComPedidos = context.Clientes
    .Include(c => c.Pedidos) // Faz o JOIN com a tabela de Pedidos
    .ToList();
```

---

### 6. Transações

Por padrão, o `SaveChanges()` já executa tudo dentro de uma transação implícita. Se você precisar envolver múltiplas operações ou múltiplos contextos:

```csharp
using var context = new AppDbContext();
using var transaction = context.Database.BeginTransaction();

try
{
    context.Clientes.Add(novoCliente);
    context.SaveChanges();

    context.Logs.Add(new Log { Mensagem = "Cliente inserido" });
    context.SaveChanges();

    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

---

### 7. Performance

### 7.1 Por que dizem que o EF Core é lento?

O EF Core carrega uma árvore de expressões complexa para traduzir LINQ em SQL e, por padrão, mantém uma cópia de cada objeto lido na memória para rastrear alterações (*Change Tracking*).

### 7.2 Como torná-lo ultra rápido (Boas práticas):

- **Use AsNoTracking():** Para consultas somente leitura (onde você não vai dar `Update`), desabilite o rastreamento. O consumo de memória despenca e a velocidade dobra.C#

```csharp
var clientesLeitura = context.Clientes.AsNoTracking().ToList();
```

- **Evite trazer todas as colunas:** Use `Select` para trazer apenas o que precisa (*Projection*).

```csharp
var nomesClientes = context.Clientes
    .Select(c => new { c.Id, c.Nome })
    .ToList();
```

- **Execute Delete/Update em massa:** Antigamente era preciso carregar para a memória para deletar. O EF Core permite fazer isso direto no banco:

```csharp
context.Clientes.Where(c => c.Ativo == false).ExecuteDelete();
```

---

### 8. Uso em ETL (seu cenário)

Historicamente, o EF Core era péssimo para ETL devido ao *overhead* do rastreamento de estado. Hoje, ele se tornou muito viável se usado corretamente:

- Use `AsNoTracking()`.
- Utilize `ExecuteUpdate` e `ExecuteDelete` para manipulação em massa sem carregar dados para a memória.
- Se precisar fazer inserções massivas, desabilite o rastreador e insira em lotes (*Batches*) de 500 a 1000 registros por `SaveChanges()`.

---

### 9. Comparação Técnica

| **Característica** | **Entity Framework** | **Dapper** | **ADO.NET** |
| --- | --- | --- | --- |
| **Performance** | Média / Alta (se otimizado) | Alta | Muito Alta |
| **Geração de SQL** | Automática | Manual | Manual |
| **Migrations** | Sim (Nativo) | Não | Não |
| **Curva de Aprendizado** | Alta | Média | Baixa |

---

### 10. Boas Práticas

- Sempre use `AsNoTracking()` em consultas de leitura.
- Não abuse do *Lazy Loading* (pode gerar o problema de N+1 consultas no banco).
- Monitore o SQL que o EF está gerando no console durante o desenvolvimento.
- Mantenha suas entidades focadas no domínio e use DTOs para tráfego de dados.

---

### 11. Armadilhas Comuns

- **Problema do N+1:** Executar uma query para listar clientes e depois disparar uma nova query de pedidos para cada cliente da lista por não usar o `Include`.
- **Esquecer do `AsNoTracking`** em telas de relatórios que carregam milhares de linhas.
- **Fazer filtros em memória:** Usar `.ToList()` antes do `.Where()`. Isso traz a tabela inteira para a memória do C# para só depois filtrar.

---

### 12. Próximos Passos

- Aprender a criar e aplicar *Migrations*.
- Dominar a API Fluente (*Fluent API*) para mapeamento avançado.
- Estudar o comportamento do *Change Tracker*.

---

## Referências e Documentação Oficial

Para aprofundar os estudos e entender as especificidades deste ORM completo, utilize as fontes oficiais da Microsoft e recursos da comunidade:

### 1. Documentação Principal
* **Visão Geral do Entity Framework Core (Microsoft Learn):** O ponto de partida oficial que explica a arquitetura, o conceito de `DbContext` e como começar a mapear suas entidades.
* **Provedores de Banco de Dados no EF Core:** Documentação sobre como conectar o EF Core a diferentes SGBDs (SQL Server, PostgreSQL, SQLite, MySQL, etc.) usando pacotes NuGet específicos.
* **Abordagem Code-First e Migrations:** Guia detalhado sobre como gerenciar o esquema do seu banco de dados diretamente através de classes C# e comandos no console.

### 2. Artigos de Comparação Técnica
* **EF Core vs Dapper (Discussões de Arquitetura):** Artigos e debates focados no equilíbrio entre a alta produtividade e abstração do EF Core contra a velocidade bruta e controle de SQL do Dapper.
* **Evolução de Performance do EF Core:** Documentação e benchmarks que mostram o salto de desempenho que o EF Core teve em relação ao antigo Entity Framework 6, aproximando-se bastante do ADO.NET em cenários comuns.

### 3. Melhores Práticas
* **Consultas No-Tracking (`AsNoTracking`):** Essencial para entender como otimizar a performance de leitura no EF Core, desativando o rastreamento de alterações quando você só precisa exibir dados.
* **Padrões de Carregamento (Eager, Explicit e Lazy Loading):** Como gerenciar o carregamento de dados relacionados de forma eficiente para evitar o clássico problema de performance conhecido como "N+1 consultas".

---
