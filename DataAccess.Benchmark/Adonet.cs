using System.Data;
using System.Data.SQLite;
using System.Diagnostics;

namespace DataAccess.Benchmark;

internal class Adonet
{
    private const string ConnectionString = "Data Source=benchmark.db;";

    // Aumenta volume drasticamente
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
                Console.Write($"\r{i:N0}/{total:N0}");
        }

        transaction.Commit();
        sw.Stop();

        Console.WriteLine($"\nTempo: {sw.Elapsed.TotalSeconds:F2}s | {total / sw.Elapsed.TotalSeconds:N0} inserts/s");
    }

    // Estresse com múltiplas conexões paralelas
    internal static async Task InsertParaleloAsync(int threads = 8, int porThread = 250_000)
    {
        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, threads).Select(t => Task.Run(() =>
        {
            // SQLite precisa de WAL para suportar escritas paralelas
            var cs = "Data Source=benchmark.db;Journal Mode=WAL;";
            using var conn = new SQLiteConnection(cs);
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

            int inicio = t * porThread;
            for (int i = inicio; i < inicio + porThread; i++)
            {
                pNome.Value = $"Nome {i}";
                pEmail.Value = $"email{i}@teste.com";
                pAtivo.Value = i % 2;
                pData.Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }));

        await Task.WhenAll(tasks);
        int total = threads * porThread;
        sw.Stop();

        Console.WriteLine($"Paralelo: {total:N0} registros em {sw.Elapsed.TotalSeconds:F2}s | {total / sw.Elapsed.TotalSeconds:N0} inserts/s");
    }

    // Batch com INSERT multi-row (estresse máximo)
    internal static async Task InsertBatch(int total = 1_000_000, int batchSize = 500)
    {
        var sw = Stopwatch.StartNew();
        using var conn = new SQLiteConnection(ConnectionString);
        conn.Open();

        using var transaction = conn.BeginTransaction();

        int inseridos = 0;
        while (inseridos < total)
        {
            int tamanho = Math.Min(batchSize, total - inseridos);

            // Monta INSERT com N linhas de uma vez
            var sb = new System.Text.StringBuilder();
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
                Console.Write($"\r{inseridos:N0}/{total:N0}");
        }

        transaction.Commit();
        sw.Stop();
        Console.WriteLine($"\nBatch: {total:N0} em {sw.Elapsed.TotalSeconds:F2}s | {total / sw.Elapsed.TotalSeconds:N0} inserts/s");
    }
}