using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using Dapper;

namespace DataAccess.Benchmark;

internal class Dapper
{

    private const string ConnectionString = "Data Source=benchmark.db;";
    private const int Total = 1_000_000;

    internal static void InsertSimplesDapper(int total = Total)
    {
        var sw = Stopwatch.StartNew();

        using var conn = new SQLiteConnection(ConnectionString);
        conn.Open();

        using var transaction = conn.BeginTransaction();

        var sql = @"
        INSERT INTO Pessoas (Nome, Email, Ativo, DataCriacao)
        VALUES (@Nome, @Email, @Ativo, @DataCriacao)";

        for (int i = 1; i <= total; i++)
        {
            conn.Execute(sql, new
            {
                Nome = $"Nome {i}",
                Email = $"email{i}@teste.com",
                Ativo = i % 2,
                DataCriacao = DateTime.UtcNow
            }, transaction);

            if (i % 10_000 == 0)
                Console.Write($"\rProgresso: {i:N0}/{total:N0}");
        }

        transaction.Commit();
        sw.Stop();

        PrintResultado("Insert Simples Dapper", total, sw.Elapsed);
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