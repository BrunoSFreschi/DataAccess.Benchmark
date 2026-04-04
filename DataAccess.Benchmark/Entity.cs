using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using static DataAccess.Benchmark.Functions;

namespace DataAccess.Benchmark;

internal class Entity
{
    internal static void InsertSimples(int total = BenchmarkConfig.Total)
    {
        var sw = Stopwatch.StartNew();

        using var context = new AppDbContext();

        // Reduz overhead interno do EF
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        using var transaction = context.Database.BeginTransaction();

        for (int i = 1; i <= total; i++)
        {
            context.Clientes.Add(new Cliente
            {
                Name = $"Nome {i}",
                Mail = $"email{i}@teste.com",
                Activated = i % 2 == 0 ? false : true,
                CreateAt = DateTime.UtcNow
            });

            context.SaveChanges();

            // ESSENCIAL: evita degradação progressiva
            context.ChangeTracker.Clear();

            Console.Write($"\rProgresso: {i:N0}/{total:N0}");

            if (i % 10_000 == 0)
                Console.Write($"\rProgresso: {i:N0}/{total:N0}");
        }

        transaction.Commit();
        sw.Stop();

        Functions.PrintResultado("Insert Simples EF (row-by-row)", total, sw.Elapsed);
    }

    internal static void InsertBatch(int total = BenchmarkConfig.Total, int batchSize = BenchmarkConfig.BatchSize)
    {
        var sw = Stopwatch.StartNew();

        using var context = new AppDbContext();

        context.ChangeTracker.AutoDetectChangesEnabled = false;

        using var transaction = context.Database.BeginTransaction();

        int inseridos = 0;

        while (inseridos < total)
        {
            int tamanho = Math.Min(batchSize, total - inseridos);

            var batch = new List<Cliente>(tamanho);

            for (int j = 0; j < tamanho; j++)
            {
                int idx = inseridos + j;

                batch.Add(new Cliente
                {
                    Name = $"Nome {idx}",
                    Mail = $"email{idx}@teste.com",
                    Activated = idx % 2 == 0,
                    CreateAt = DateTime.UtcNow
                });
            }

            context.Clientes.AddRange(batch);

            context.SaveChanges();

            context.ChangeTracker.Clear();

            inseridos += tamanho;

            if (inseridos % 50_000 == 0)
                Console.Write($"\rProgresso: {inseridos:N0}/{total:N0}");
        }

        transaction.Commit();
        sw.Stop();

        Functions.PrintResultado($"Insert Batch EF (batchSize={batchSize})", total, sw.Elapsed);
    }

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

                using var context = new AppDbContext();

                // PRAGMA para reduzir lock
                context.Database.ExecuteSqlRaw("PRAGMA busy_timeout = 5000;");
                context.Database.ExecuteSqlRaw("PRAGMA journal_mode = WAL;");
                context.Database.ExecuteSqlRaw("PRAGMA synchronous = NORMAL;");

                context.ChangeTracker.AutoDetectChangesEnabled = false;

                for (int i = inicio; i <= fim; i += batchSize)
                {
                    int tamanho = Math.Min(batchSize, fim - i + 1);

                    bool inserido = false;
                    int tentativa = 0;

                    while (!inserido)
                    {
                        try
                        {
                            using var tx = context.Database.BeginTransaction();

                            var batch = new List<Cliente>(tamanho);

                            for (int j = 0; j < tamanho; j++)
                            {
                                int idx = i + j;

                                batch.Add(new Cliente
                                {
                                    Name = $"Nome {idx}",
                                    Mail = $"email{idx}@teste.com",
                                    Activated = idx % 2 == 0,
                                    CreateAt = DateTime.UtcNow
                                });
                            }

                            context.Clientes.AddRange(batch);
                            context.SaveChanges();

                            tx.Commit();

                            context.ChangeTracker.Clear();

                            int atual = Interlocked.Add(ref progresso, tamanho);

                            if (atual % 50_000 == 0)
                            {
                                lock (consoleLock)
                                    Console.Write($"\rProgresso: {atual:N0}/{total:N0}");
                            }

                            inserido = true;
                        }
                        catch (Exception ex) when (
                            ex.InnerException is Microsoft.Data.Sqlite.SqliteException sqliteEx &&
                            (sqliteEx.SqliteErrorCode == 5 || // Busy
                             sqliteEx.SqliteErrorCode == 6))  // Locked
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

        Functions.PrintResultado(
            $"Insert Paralelo EF (threads={grauParalelismo}, batchSize={batchSize})",
            total,
            sw.Elapsed);
    }
}