using System.Data;
using System.Data.SQLite;
using System.Diagnostics;

namespace DataAccess.Benchmark;

internal class Dapper
{
    private const string ConnectionString = "Data Source=benchmark.db;";
    private const int Total = 1_000_000;
    private const int BatchSize = 500;


    internal static void InsertSimples(int total = Total)
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
                Console.Write($"\rProgresso: {i:N0}/{total:N0}");
        }

        transaction.Commit();
        sw.Stop();

        PrintResultado("Insert Simples", total, sw.Elapsed);
    }

    private static void PrintResultado(string descricao, int total, TimeSpan tempo)
    {
        Console.WriteLine();
        Console.WriteLine($"[{descricao}]");
        Console.WriteLine($"Registros: {total:N0}");
        Console.WriteLine($"Tempo: {tempo.TotalSeconds:F2}s");
        Console.WriteLine($"Taxa: {total / tempo.TotalSeconds:N0} registros/s");
        Console.WriteLine();
    }
}