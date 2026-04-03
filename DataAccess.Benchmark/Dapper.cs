using Dapper;
using System.Data.SQLite;
using System.Diagnostics;

namespace DataAccess.Benchmark;

internal class Dapper
{

    private const string ConnectionString = "Data Source=benchmark.db;";
    private const int Total = 1_000_000;

    internal static void InsertBatch(int total = Total, int batchSize = 1000)
    {
        var sw = Stopwatch.StartNew();

        using var connection = new SQLiteConnection(ConnectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        var sql = @"
        INSERT INTO Pessoas (Nome, Email, Ativo, DataCriacao)
        VALUES (@Nome, @Email, @Ativo, @DataCriacao)";

        var batch = new List<object>(batchSize);

        for (int i = 1; i <= total; i++)
        {
            batch.Add(new
            {
                Nome = $"Nome {i}",
                Email = $"email{i}@teste.com",
                Ativo = i % 2,
                DataCriacao = DateTime.UtcNow
            });

            if (batch.Count == batchSize)
            {
                connection.Execute(sql, batch, transaction);
                batch.Clear();

                Console.Write($"\rProgresso: {i:N0}/{total:N0}");
            }
        }

        if (batch.Count > 0)
        {
            connection.Execute(sql, batch, transaction);
        }

        transaction.Commit();
        sw.Stop();

        Messages.PrintResultado("Insert Batch Dapper", total, sw.Elapsed);
    }

    internal static void InsertSimples(int total = Total)
    {
        var sw = Stopwatch.StartNew();

        using var connection = new SQLiteConnection(ConnectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        var sql = @"
            INSERT INTO Pessoas (Nome, Email, Ativo, DataCriacao)
            VALUES (@Nome, @Email, @Ativo, @DataCriacao)";

        for (int i = 1; i <= total; i++)
        {
            connection.Execute(sql, new
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

        Messages.PrintResultado("Insert Simples Dapper", total, sw.Elapsed);
    }
}