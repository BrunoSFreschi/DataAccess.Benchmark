O ADO.NET é o "pai" a base sobre a qual o Dapper foi construído. Entender o ADO.NET a fundo te dá o controle mais absoluto possível sobre o banco de dados no ecossistema .NET.

Este é um guia, estruturado totalmente focado no **ADO.NET**.

---

## ADO.NET — Guia Prático

### 1. Visão Geral

O ADO.NET (ActiveX Data Objects para .NET) é o conjunto de classes nativo do .NET Framework e .NET para comunicação direta com fontes de dados. Ele é a fundação de quase todos os ORMs do ecossistema (incluindo Entity Framework e Dapper).

Ele foi projetado para oferecer:

- **Performance máxima** (é o nível mais baixo de abstração antes do driver do banco)
- **Controle total e granular** sobre conexões, comandos e transações
- **Arquitetura desconectada** (com `DataSet` e `DataTable`) e **conectada** (com `DataReader`)

Diferente de ORMs, o ADO.NET não faz mapeamento automático de objetos. Você precisa ler os dados linha por linha e coluna por coluna manualmente.

### Quando usar ADO.NET

Use ADO.NET quando:

- **Performance extrema** e milisegundos importam (ex: operações em massa, Bulk Insert nativo)
- Você precisa usar recursos ultra específicos do banco de dados (como tipos de dados proprietários)
- Está criando uma biblioteca de acesso a dados ou um Micro-ORM próprio
- O ambiente restringe o uso de bibliotecas externas (zero dependências)

### Evite quando:

- Não quer escrever código repetitivo (*boilerplate*) para mapear tabelas para objetos
- O projeto exige desenvolvimento extremamente rápido de CRUDs
- Manutenibilidade do mapeamento manual se tornar um fardo

---

### 2. Instalação / Uso

O ADO.NET já vem integrado ao .NET. No entanto, para bancos específicos, você precisa instalar o provedor de dados (*DataProvider*) via CLI ou NuGet.

**Para SQL Server:**

```bash
dotnet add package Microsoft.Data.SqlClient
```

**Para PostgreSQL:**

```bash
dotnet add package Npgsql
```

---

### 3. Conceitos Fundamentais

### 3.1 Os Quatro Pilares Conectados

O ADO.NET baseia-se em quatro classes principais (usando SQL Server como exemplo):

- `SqlConnection`: Gerencia a conexão com o banco.
- `SqlCommand`: Executa a instrução SQL ou Stored Procedure.
- `SqlDataReader`: Lê os dados resultantes de forma ultra rápida e somente para frente (*forward-only*).
- `SqlDataAdapter`: Preenche objetos desconectados (`DataSet`/`DataTable`).

### 3.2 Executando uma Query (SELECT)

Como o ADO.NET não mapeia objetos sozinho, você precisa fazer o laço manual:

```csharp
using (var connection = new SqlConnection(connectionString))
{
    var command = new SqlCommand("SELECT Id, Nome, Email FROM Clientes", connection);
    connection.Open();

    using (var reader = command.ExecuteReader())
    {
        var clientes = new List<Cliente>();
        while (reader.Read())
        {
            clientes.Add(new Cliente
            {
                Id = reader.GetInt32(0),
                Nome = reader.GetString(1),
                Email = reader.GetString(2)
            });
        }
    }
}
```

### 3.3 ExecuteScalar e ExecuteNonQuery

O ADO.NET divide a execução de comandos pelo tipo de retorno esperado:

**ExecuteScalar:** Retorna a primeira coluna da primeira linha. Ótimo para funções de agregação ou IDs gerados.

```csharp
var command = new SqlCommand("SELECT COUNT(*) FROM Clientes", connection);
int total = (int)command.ExecuteScalar();
```

**ExecuteNonQuery:** Usado para INSERT, UPDATE e DELETE.

```csharp
var command = new SqlCommand("UPDATE Clientes SET Nome = 'Fulano' WHERE Id = 1", connection);
int linhasAfetadas = command.ExecuteNonQuery(); // Retorna o número de linhas afetadas
```

---

### 4. Parâmetros e Segurança

### 4.1 Evitando SQL Injection

O ADO.NET exige a adição manual de parâmetros ao objeto `SqlCommand`.

```csharp
var command = new SqlCommand("SELECT * FROM Clientes WHERE Email = @Email", connection);
command.Parameters.AddWithValue("@Email", email);

using (var reader = command.ExecuteReader()) { /* ... */ }
```

### 4.2 Nunca faça isso:

```csharp
// ERRADO - Extremamente vulnerável e péssimo para o cache de execução do banco
var command = new SqlCommand($"SELECT * FROM Clientes WHERE Email = '{email}'", connection);
```

---

### 5. Recursos Avançados

### 5.1 Trabalhando com Stored Procedures

O ADO.NET brilha ao trabalhar com Procedures:

```csharp
var command = new SqlCommand("Sp_BuscarCliente", connection);
command.CommandType = CommandType.StoredProcedure;
command.Parameters.AddWithValue("@Id", 1);

// Parâmetro de saída (Output)
SqlParameter outParam = new SqlParameter("@NomeLogradouro", SqlDbType.VarChar, 100) 
{ 
    Direction = ParameterDirection.Output 
};
command.Parameters.Add(outParam);

command.ExecuteNonQuery();
string nomeRua = outParam.Value.ToString();
```

### 5.2 Modo Desconectado (DataTable / DataSet)

Útil quando você precisa manipular os dados em memória e depois sincronizar tudo de uma vez com o banco.

```csharp
var adapter = new SqlDataAdapter("SELECT * FROM Clientes", connection);
var dataTable = new DataTable();
adapter.Fill(dataTable); // Preenche a tabela em memória

foreach (DataRow row in dataTable.Rows)
{
    Console.WriteLine(row["Nome"]);
}
```

---

### 6. Transações

No ADO.NET, a transação deve ser explicitamente associada ao comando.

```csharp
using var connection = new SqlConnection(connectionString);
connection.Open();

using var transaction = connection.BeginTransaction();
using var command = connection.CreateCommand();
command.Transaction = transaction;

try
{
    command.CommandText = "INSERT INTO Log... ";
    command.ExecuteNonQuery();

    command.CommandText = "UPDATE Saldo... ";
    command.ExecuteNonQuery();

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

### 7.1 Por que o ADO.NET é o mais rápido?

- **Sem reflexão pesada:** Você diz exatamente de onde tirar o dado e para onde colocá-lo.
- **Zero processamento intermediário:** Não há geração de SQL dinâmica ou árvores de expressão.
- **Streaming de dados:** O `DataReader` não carrega tudo na memória; ele lê o fluxo da rede linha por linha.

### 7.2 Boas práticas de performance

- **Sempre feche as conexões:** Utilize o bloco `using` para garantir que a conexão volte ao *Connection Pool*.
- Use os métodos tipados do Reader (ex: `reader.GetInt32(0)` em vez de `(int)reader["Id"]`) pois evitam *boxing/unboxing*.
- Mantenha o menor tempo de conexão aberta possível.

---

### 8. Uso em ETL (seu cenário)

Para cenários de carga de dados massiva (ETL), o ADO.NET possui a ferramenta definitiva no ecossistema .NET: o **SqlBulkCopy** (exclusivo para SQL Server).

```csharp
using (var bulkCopy = new SqlBulkCopy(connectionDestino))
{
    bulkCopy.DestinationTableName = "ClientesDestino";
    
    // Pode receber um DataTable ou diretamente um IDataReader
    bulkCopy.WriteToServer(readerOrigem); 
}
```

*O SqlBulkCopy ignora boa parte do overhead de inserção e joga os dados direto nas páginas do banco, sendo ordens de magnitude mais rápido que loops de INSERT.*

---

### 9. Comparação Técnica

| **Característica** | **ADO.NET** | **Dapper** | **Entity Framework** |
| --- | --- | --- | --- |
| **Performance** | Máxima | Alta | Média |
| **Controle SQL** | Total | Total | Parcial |
| **Facilidade** | Baixa (Muito código) | Média | Alta |
| **Mapeamento de Objetos** | Manual | Automático | Automático |
| **Geração de SQL** | Nenhuma | Nenhuma | Automática |

---

### 10. Boas Práticas

- Sempre use blocos `using` para conexões, comandos e leitores.
- Nunca concatene strings para montar queries; use sempre parâmetros.
- Prefira o modo conectado (`DataReader`) para grandes volumes de dados para poupar memória.
- Encapsule esse código feio de leitura manual dentro de classes de repositório para não poluir sua regra de negócio.

---

### 11. Armadilhas Comuns

- **Esquecer de associar a Transaction ao Command:** Isso gera um erro clássico em tempo de execução.
- **Não fechar o DataReader:** Bloqueia a conexão e impede que outras queries sejam executadas na mesma conexão.
- **Excesso de código duplicado:** Sem cuidado, o código ADO.NET vira um festival de copiar e colar.

---

### 12. Próximos Passos

- Experimentar o `SqlBulkCopy` para migrar dados entre tabelas.
- Criar métodos de extensão para automatizar as leituras do `DataReader`.
- Entender o funcionamento do *Connection Pooling* no arquivo de configuração.

---

## Referências e Documentação Oficial

Para aprofundar os estudos e entender as especificidades de cada provedor de dados, utilize as fontes oficiais da Microsoft e recursos da comunidade:

### 1. Documentação Principal

- [Visão Geral do ADO.NET (Microsoft Learn)](https://learn.microsoft.com/pt-br/dotnet/framework/data/adonet/ado-net-overview): A porta de entrada para entender a arquitetura e os componentes principais.
- [System.Data.SqlClient Namespace](https://www.google.com/search?q=https://learn.microsoft.com/pt-br/dotnet/api/system.data.sqlclient&authuser=1): Documentação detalhada das classes específicas para SQL Server.
- [Provedores de Dados do .NET](https://learn.microsoft.com/pt-br/dotnet/framework/data/adonet/data-providers): Lista de drivers para diferentes bancos de dados (Oracle, OLE DB, ODBC, etc).

### 2. Artigos de Comparação Técnica

- [ADO.NET vs. Entity Framework (Stack Overflow)](https://www.google.com/search?q=https://stackoverflow.com/questions/1126760/adonet-vs-entity-framework&authuser=1): Uma das discussões mais ricas sobre quando escolher cada abordagem.
- [Performance Benchmark (Dapper vs EF vs ADO.NET)](https://www.google.com/search?q=https://github.com/StackExchange/Dapper%23performance&authuser=1): O repositório oficial do Dapper mantém uma tabela comparativa de performance onde o ADO.NET é o "baseline" (padrão de referência).

### 3. Melhores Práticas

- [Gerenciamento de Conexões e Pooling](https://learn.microsoft.com/pt-br/dotnet/framework/data/adonet/sql-server-connection-pooling): Essencial para entender como o ADO.NET otimiza a abertura e fechamento de conexões com o banco.
- [Segurança: Evitando SQL Injection](https://www.google.com/search?q=https://learn.microsoft.com/pt-br/dotnet/framework/data/adonet/commands-and-parameters&authuser=1): Guia sobre como utilizar o `SqlParameter` corretamente no ADO.NET.

---
