using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Text;

namespace DataAccess.Benchmark;

internal class Adonet
{
    private const string ConnectionString = "Data Source=benchmark.db;";

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
    /*
     Resultado esperado: o modo paralelo será mais lento que o batch serial no SQLite. 
     Isso é normal — SQLite serializa escritas internamente. 
     O benchmark serve justamente para demonstrar que paralelismo nem sempre é vantagem com SQLite. 
     O overhead do lock contention consome o ganho teórico.
    */
    internal static void InsertParaleloSimples(int total = 1_000_000, int grauParalelismo = 4, int batchSize = 500)
    {
        var sw = Stopwatch.StartNew();
        int progresso = 0;
        object consoleLock = new object();

        var tasks = Enumerable.Range(0, grauParalelismo).Select(worker =>
            Task.Run(() =>
            {
                int porWorker = total / grauParalelismo;
                int inicio = worker * porWorker + 1;
                int fim = (worker == grauParalelismo - 1) ? total : inicio + porWorker - 1;

                using var conn = new SQLiteConnection(ConnectionString);
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
                                    Console.Write($"\r  Progresso: {atual:N0}/{total:N0}");
                            }

                            inserido = true;
                        }
                        catch (SQLiteException ex) when (
                            ex.ResultCode == SQLiteErrorCode.Busy ||
                            ex.ResultCode == SQLiteErrorCode.Locked)
                        {
                            tentativa++;
                            if (tentativa > 10) throw;
                            Thread.Sleep(50 * tentativa);
                        }
                    }
                }
            })
        ).ToArray();

        Task.WaitAll(tasks);

        sw.Stop();
        Console.WriteLine($"\n✓ Inserts Paralelos: {total:N0} registros em {sw.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"  Taxa: {total / sw.Elapsed.TotalSeconds:N0} inserts/s\n");
    }

}