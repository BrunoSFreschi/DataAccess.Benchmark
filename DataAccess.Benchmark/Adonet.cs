using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Text;

namespace DataAccess.Benchmark;

internal class Adonet
{
    private const string ConnectionString = "Data Source=benchmark.db;";

    // Inicializa banco com WAL ativado
    internal static void InicializarBanco()
    {
        using var conn = new SQLiteConnection(ConnectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL;";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "PRAGMA synchronous=NORMAL;";
        cmd.ExecuteNonQuery();

        Console.WriteLine("- Banco inicializado com WAL e otimizações");
    }

    // Simples - sem paralelismo (baseline)
    internal static void InsertClientes(int total = 1_000_000)
    {
        var sw = Stopwatch.StartNew();
        using var conn = new SQLiteConnection(ConnectionString);
        conn.Open();

        using var transaction = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO Pessoas (Nome, Email, Ativo, DataCriacao)
            VALUES (@Nome, @Email, @Ativo, @DataCriacao)";

        var pNome = cmd.Parameters.Add("@Nome", DbType.String);
        var pEmail = cmd.Parameters.Add("@Email", DbType.String);
        var pAtivo = cmd.Parameters.Add("@Ativo", DbType.Int32);
        var pData = cmd.Parameters.Add("@DataCriacao", DbType.String);

        for (int i = 1; i <= total; i++)
        {
            pNome.Value = $"Nome {i}";
            pEmail.Value = $"email{i}@teste.com";
            pAtivo.Value = i % 2;
            pData.Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            cmd.ExecuteNonQuery();

            if (i % 10_000 == 0)
                Console.Write($"\r  Progresso: {i:N0}/{total:N0}");
        }

        transaction.Commit();
        sw.Stop();

        Console.WriteLine($"\n✓ Inserts Simples: {total:N0} registros em {sw.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"  Taxa: {total / sw.Elapsed.TotalSeconds:N0} inserts/s\n");
    }

    // Batch com INSERT multi-row (mais rápido)
    internal static void InsertBatch(int total = 1_000_000, int batchSize = 500)
    {
        var sw = Stopwatch.StartNew();
        using var conn = new SQLiteConnection(ConnectionString);
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
                Console.Write($"\r  Progresso: {inseridos:N0}/{total:N0}");
        }

        transaction.Commit();
        sw.Stop();

        Console.WriteLine($"\n✓ Inserts em Batch (tamanho {batchSize}): {total:N0} registros em {sw.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"  Taxa: {total / sw.Elapsed.TotalSeconds:N0} inserts/s\n");
    }

    // Paralelo - múltiplas conexões (com lock handling)
    internal static async Task InsertParaleloAsync(int threads = 2, int quantidadePorThread = 1_000_000)
    {
        var sw = Stopwatch.StartNew();

        // ─── 1. Configurar WAL mode ANTES de iniciar as threads ───
        using (var connSetup = new SQLiteConnection(ConnectionString))
        {
            connSetup.Open();
            using var cmdSetup = connSetup.CreateCommand();
            cmdSetup.CommandText = "PRAGMA journal_mode=WAL;";
            cmdSetup.ExecuteNonQuery();

            // Aumenta o timeout para locks
            cmdSetup.CommandText = "PRAGMA busy_timeout=30000;";
            cmdSetup.ExecuteNonQuery();
        }

        // ─── 2. Semáforo para limitar escritas simultâneas ───
        // SQLite WAL permite 1 writer por vez, mas podemos enfileirar
        var semaphore = new SemaphoreSlim(1); // ← IMPORTANTE: 1, não 2!

        int totalInseridos = 0;
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, threads).Select(t => Task.Run(async () =>
        {
            await semaphore.WaitAsync();
            try
            {
                using var conn = new SQLiteConnection(ConnectionString);
                conn.Open();

                // Configurar busy_timeout por conexão
                using (var pragmaCmd = conn.CreateCommand())
                {
                    pragmaCmd.CommandText = "PRAGMA busy_timeout=30000;";
                    pragmaCmd.ExecuteNonQuery();
                }

                using var transaction = conn.BeginTransaction();
                using var cmd = conn.CreateCommand();

                // ─── 3. Associar o comando à transação explicitamente ───
                cmd.Transaction = transaction;
                cmd.CommandText = @"
                INSERT INTO Pessoas (Nome, Email, Ativo, DataCriacao)
                VALUES (@Nome, @Email, @Ativo, @DataCriacao)";

                var pNome = cmd.Parameters.Add("@Nome", DbType.String);
                var pEmail = cmd.Parameters.Add("@Email", DbType.String);
                var pAtivo = cmd.Parameters.Add("@Ativo", DbType.Int32);
                var pData = cmd.Parameters.Add("@DataCriacao", DbType.String);

                // ─── 4. Preparar o comando para melhor performance ───
                cmd.Prepare();

                int inicio = t * quantidadePorThread;
                int count = 0;

                for (int i = inicio; i < inicio + quantidadePorThread; i++)
                {
                    pNome.Value = $"Nome {i}";
                    pEmail.Value = $"email{i}@teste.com";
                    pAtivo.Value = i % 2;
                    pData.Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                    cmd.ExecuteNonQuery();
                    count++;
                }

                // ─── 5. Commit DENTRO do try ───
                transaction.Commit();
                Interlocked.Add(ref totalInseridos, count);

                Console.WriteLine($"  Thread {t}: {count:N0} registros inseridos ✓");
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                Console.WriteLine($"  Thread {t}: ERRO - {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        }));

        await Task.WhenAll(tasks);
        sw.Stop();

        // ─── 6. Reportar erros se houver ───
        if (!exceptions.IsEmpty)
        {
            Console.WriteLine($"  ⚠ {exceptions.Count} threads falharam!");
            foreach (var ex in exceptions)
                Console.WriteLine($"    → {ex.Message}");
        }

        Console.WriteLine($"✓ Inserts Paralelos ({threads} threads): " +
                          $"{totalInseridos:N0} registros em {sw.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"  Taxa: {totalInseridos / sw.Elapsed.TotalSeconds:N0} inserts/s\n");
    }
}