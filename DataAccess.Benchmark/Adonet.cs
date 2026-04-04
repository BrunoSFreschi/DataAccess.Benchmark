using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Text;
using static DataAccess.Benchmark.Functions;

namespace DataAccess.Benchmark;

internal class Adonet
{
    /// <summary>
    /// Realiza a inserção de registros na tabela <c>Pessoas</c> de forma sequencial,
    /// utilizando ADO.NET com comandos parametrizados dentro de uma única transação.
    /// </summary>
    /// <param name="total">
    /// Quantidade total de registros a serem inseridos.
    /// O valor padrão é definido pela constante <c>Total</c>.
    /// </param>
    /// <remarks>
    /// <para>
    /// Este método representa o cenário mais simples de inserção (baseline),
    /// onde cada registro é persistido individualmente através de chamadas
    /// consecutivas ao método <c>ExecuteNonQuery</c>.
    /// </para>
    ///
    /// <para>
    /// Apesar de utilizar uma transação única (o que reduz significativamente o overhead de I/O),
    /// ainda é menos eficiente que abordagens em lote (<c>batch</c>), pois realiza
    /// múltiplas execuções de comando no banco.
    /// </para>
    ///
    /// <para>
    /// O uso de parâmetros reutilizáveis (<c>SQLiteParameter</c>) evita recriação de parâmetros
    /// a cada iteração, reduzindo overhead de alocação e melhorando a performance.
    /// </para>
    ///
    /// <para>
    /// Este método é ideal para:
    /// <list type="bullet">
    /// <item><description>Estabelecer baseline de performance</description></item>
    /// <item><description>Comparação com estratégias otimizadas (batch, paralelismo)</description></item>
    /// <item><description>Cenários simples de inserção com baixo volume de dados</description></item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// Durante a execução, o progresso é exibido no console a cada 10.000 registros.
    /// </para>
    /// </remarks>
    /// <exception cref="SQLiteException">
    /// Lançada em caso de falha na execução do comando SQL ou problemas de conexão.
    /// </exception>
    /// <example>
    /// Exemplo de uso:
    /// <code>
    /// InsertSimples(100_000);
    /// </code>
    /// </example>
    /// <seealso cref="InsertBatch"/>
    /// <seealso cref="InsertParalelo"/>
    internal static void InsertSimples(int total = BenchmarkConfig.Total)
    {
        var sw = Stopwatch.StartNew();

        using var connection = new SQLiteConnection(BenchmarkConfig.ConnectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();
        using var cmd = connection.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO Pessoas (Nome, Email, Ativo, DataCriacao)
            VALUES (@Nome, @Email, @Ativo, @DataCriacao)";

        var nome = cmd.Parameters.Add("@Nome", DbType.String);
        var email = cmd.Parameters.Add("@Email", DbType.String);
        var ativo = cmd.Parameters.Add("@Ativo", DbType.Int32);
        var data = cmd.Parameters.Add("@DataCriacao", DbType.String);

        for (int i = 1; i <= total; i++)
        {
            nome.Value = $"Nome {i}";
            email.Value = $"email{i}@teste.com";
            ativo.Value = i % 2;
            data.Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            cmd.ExecuteNonQuery();

            if (i % 10_000 == 0)
                Console.Write($"\rProgresso: {i:N0}/{total:N0}");
        }

        transaction.Commit();
        sw.Stop();

        Functions.PrintResultado("Insert Simples", total, sw.Elapsed);
    }

    /// <summary>
    /// Realiza a inserção de registros na tabela <c>Pessoas</c> utilizando inserções em lote
    /// (multi-row insert), reduzindo a quantidade de comandos enviados ao banco de dados.
    /// </summary>
    /// <param name="total">
    /// Quantidade total de registros a serem inseridos.
    /// O valor padrão é definido pela constante <c>Total</c>.
    /// </param>
    /// <param name="batchSize">
    /// Quantidade de registros por lote (batch).
    /// O valor padrão é definido pela constante <c>BatchSize</c>.
    /// </param>
    /// <remarks>
    /// <para>
    /// Este método utiliza a estratégia de inserção em lote (<c>batch insert</c>),
    /// onde múltiplos registros são inseridos em uma única instrução SQL.
    /// Isso reduz significativamente o número de chamadas ao banco de dados,
    /// melhorando a performance em comparação com inserções individuais.
    /// </para>
    ///
    /// <para>
    /// A instrução SQL é construída dinamicamente, concatenando múltiplos valores
    /// no formato <c>(@N0,@E0,@A0,@D0), (@N1,@E1,@A1,@D1), ...</c>.
    /// Cada valor é parametrizado para evitar SQL Injection e manter consistência.
    /// </para>
    ///
    /// <para>
    /// Toda a operação é executada dentro de uma única transação,
    /// garantindo atomicidade e reduzindo o overhead de commit.
    /// </para>
    ///
    /// <para>
    /// Este método é ideal para:
    /// <list type="bullet">
    /// <item><description>Alto volume de inserções</description></item>
    /// <item><description>Cenários de ETL e carga de dados</description></item>
    /// <item><description>Maximização de throughput em SQLite</description></item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// O tamanho do batch impacta diretamente a performance:
    /// <list type="bullet">
    /// <item><description>Batches maiores → menos round-trips, maior throughput</description></item>
    /// <item><description>Batches muito grandes → aumento de uso de memória e tamanho do comando</description></item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// Durante a execução, o progresso é exibido no console a cada 50.000 registros.
    /// </para>
    /// </remarks>
    /// <exception cref="SQLiteException">
    /// Lançada em caso de erro na execução do comando SQL ou violação de limites do SQLite.
    /// </exception>
    /// <example>
    /// Exemplo de uso:
    /// <code>
    /// InsertBatch(1_000_000, 500);
    /// </code>
    /// </example>
    /// <seealso cref="InsertSimples"/>
    /// <seealso cref="InsertParalelo"/>
    internal static void InsertBatch(int total = BenchmarkConfig.Total, int batchSize = BenchmarkConfig.BatchSize)
    {
        var sw = Stopwatch.StartNew();

        using var conn = new SQLiteConnection(BenchmarkConfig.ConnectionString);
        conn.Open();

        using var transaction = conn.BeginTransaction();

        int inseridos = 0;

        while (inseridos < total)
        {
            int tamanho = Math.Min(batchSize, total - inseridos);

            var sb = new StringBuilder();
            sb.Append("INSERT INTO Pessoas (Nome, Email, Ativo, DataCriacao) VALUES ");

            using var cmd = conn.CreateCommand();

            for (int j = 0; j < tamanho; j++)
            {
                if (j > 0) sb.Append(',');

                sb.Append($"(@N{j},@E{j},@A{j},@D{j})");

                int idx = inseridos + j;

                cmd.Parameters.AddWithValue($"@N{j}", $"Nome {idx}");
                cmd.Parameters.AddWithValue($"@E{j}", $"email{idx}@teste.com");
                cmd.Parameters.AddWithValue($"@A{j}", idx % 2);
                cmd.Parameters.AddWithValue($"@D{j}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            cmd.CommandText = sb.ToString();
            cmd.ExecuteNonQuery();

            inseridos += tamanho;

            if (inseridos % 50_000 == 0)
                Console.Write($"\rProgresso: {inseridos:N0}/{total:N0}");
        }

        transaction.Commit();
        sw.Stop();

        Functions.PrintResultado($"Insert Batch (batchSize={batchSize})", total, sw.Elapsed);
    }

    /// <summary>
    /// Realiza a inserção de registros na tabela <c>Pessoas</c> utilizando múltiplas threads,
    /// com divisão de carga e inserções em lote por conexão.
    /// </summary>
    /// <param name="total">
    /// Quantidade total de registros a serem inseridos.
    /// O valor padrão é definido pela constante <c>Total</c>.
    /// </param>
    /// <param name="grauParalelismo">
    /// Quantidade de tarefas (threads) concorrentes utilizadas na execução.
    /// </param>
    /// <param name="batchSize">
    /// Quantidade de registros por lote em cada operação.
    /// O valor padrão é definido pela constante <c>BatchSize</c>.
    /// </param>
    /// <remarks>
    /// <para>
    /// Este método implementa paralelismo explícito utilizando múltiplas tarefas (<c>Task</c>),
    /// onde cada worker é responsável por uma partição do conjunto total de dados.
    /// </para>
    ///
    /// <para>
    /// Cada thread mantém sua própria conexão com o banco de dados e executa inserções em lote,
    /// dentro de transações independentes.
    /// </para>
    ///
    /// <para>
    /// Para lidar com contenção de escrita (<c>lock contention</c>) no SQLite,
    /// o método configura <c>PRAGMA busy_timeout</c> e implementa política de retry
    /// com backoff incremental em caso de erro (<c>Busy</c> ou <c>Locked</c>).
    /// </para>
    ///
    /// <para>
    /// Importante: SQLite permite apenas um escritor por vez.
    /// Portanto, múltiplas threads competem pelo lock de escrita,
    /// o que pode reduzir a eficiência do paralelismo.
    /// </para>
    ///
    /// <para>
    /// Na prática, este método tende a ser:
    /// <list type="bullet">
    /// <item><description>Mais lento que <c>InsertBatch</c> em SQLite</description></item>
    /// <item><description>Útil para demonstrar contenção de recursos</description></item>
    /// <item><description>Didático para estudo de concorrência e I/O bound</description></item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// O progresso global é controlado de forma thread-safe utilizando <c>Interlocked</c>
    /// e exibido no console periodicamente.
    /// </para>
    /// </remarks>
    /// <exception cref="SQLiteException">
    /// Lançada quando o número máximo de tentativas de retry é excedido
    /// ou ocorre falha crítica de escrita.
    /// </exception>
    /// <example>
    /// Exemplo de uso:
    /// <code>
    /// InsertParalelo(1_000_000, 4, 500);
    /// </code>
    /// </example>
    /// <seealso cref="InsertSimples"/>
    /// <seealso cref="InsertBatch"/>
    internal static void InsertParalelo(int total = BenchmarkConfig.Total, int grauParalelismo = 4, int batchSize = BenchmarkConfig.BatchSize)
    {
        var sw = Stopwatch.StartNew();

        int progresso = 0;
        object consoleLock = new();

        var tasks = Enumerable.Range(0, grauParalelismo).Select(worker =>
            Task.Run(() =>
            {
                int porWorker = total / grauParalelismo;
                int inicio = worker * porWorker + 1;
                int fim = (worker == grauParalelismo - 1) ? total : inicio + porWorker - 1;

                using var conn = new SQLiteConnection(BenchmarkConfig.ConnectionString);
                conn.Open();

                using var pragma = conn.CreateCommand();
                pragma.CommandText = "PRAGMA busy_timeout = 5000;";
                pragma.ExecuteNonQuery();

                for (int i = inicio; i <= fim; i += batchSize)
                {
                    int tamanho = Math.Min(batchSize, fim - i + 1);

                    bool inserido = false;
                    int tentativa = 0;

                    while (!inserido)
                    {
                        try
                        {
                            using var tx = conn.BeginTransaction();
                            using var cmd = conn.CreateCommand();

                            var sb = new StringBuilder("INSERT INTO Pessoas (Nome, Email, Ativo, DataCriacao) VALUES ");

                            for (int j = 0; j < tamanho; j++)
                            {
                                if (j > 0) sb.Append(",");

                                sb.Append($"(@N{j},@E{j},@A{j},@D{j})");

                                int idx = i + j;

                                cmd.Parameters.AddWithValue($"@N{j}", $"Nome {idx}");
                                cmd.Parameters.AddWithValue($"@E{j}", $"email{idx}@teste.com");
                                cmd.Parameters.AddWithValue($"@A{j}", idx % 2);
                                cmd.Parameters.AddWithValue($"@D{j}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                            }

                            cmd.CommandText = sb.ToString();
                            cmd.Transaction = tx;

                            cmd.ExecuteNonQuery();
                            tx.Commit();

                            int atual = Interlocked.Add(ref progresso, tamanho);

                            if (atual % 50_000 == 0)
                            {
                                lock (consoleLock)
                                    Console.Write($"\rProgresso: {atual:N0}/{total:N0}");
                            }

                            inserido = true;
                        }
                        catch (SQLiteException ex) when (
                            ex.ResultCode == SQLiteErrorCode.Busy ||
                            ex.ResultCode == SQLiteErrorCode.Locked)
                        {
                            tentativa++;

                            if (tentativa > 10)
                                throw;

                            Thread.Sleep(50 * tentativa);
                        }
                    }
                }
            })
        ).ToArray();

        Task.WaitAll(tasks);

        sw.Stop();

        Functions.PrintResultado($"Insert Paralelo (threads={grauParalelismo}, batchSize={batchSize})", total, sw.Elapsed);
    }
}