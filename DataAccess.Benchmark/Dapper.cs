using Dapper;
using System.Data.SQLite;
using System.Diagnostics;

namespace DataAccess.Benchmark;

internal class Dapper
{

    private const string ConnectionString = "Data Source=benchmark.db;";
    private const int Total = 1_000_000;
    private const int BatchSize = 500;

    internal static void InsertBatch(int total = Total, int batchSize = BatchSize)
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

    internal static void InsertParalelo(int total = Total, int grauParalelismo = 4, int batchSize = BatchSize)
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

                using var conn = new SQLiteConnection(ConnectionString);
                conn.Open();

                conn.Execute("PRAGMA busy_timeout = 5000;");

                var sql = @"
                INSERT INTO Pessoas (Nome, Email, Ativo, DataCriacao)
                VALUES (@Nome, @Email, @Ativo, @DataCriacao)";

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

                            var batch = new List<object>(tamanho);

                            for (int j = 0; j < tamanho; j++)
                            {
                                int idx = i + j;

                                batch.Add(new
                                {
                                    Nome = $"Nome {idx}",
                                    Email = $"email{idx}@teste.com",
                                    Ativo = idx % 2,
                                    DataCriacao = DateTime.UtcNow
                                });
                            }

                            conn.Execute(sql, batch, tx);

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

        Messages.PrintResultado(
            $"Insert Paralelo Dapper (threads={grauParalelismo}, batchSize={batchSize})",
            total,
            sw.Elapsed);
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