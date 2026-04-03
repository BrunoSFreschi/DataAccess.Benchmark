using System.Diagnostics;

namespace DataAccess.Benchmark;

internal class Entity
{
    private const string ConnectionString = "Data Source=benchmark.db;";
    private const int Total = 1_000_000;

    internal static void InsertBatch(int total = Total)
    {
        throw new NotImplementedException();
    }

    internal static void InsertParalelo(int total = Total)
    {
        throw new NotImplementedException();
    }

    internal static void InsertSimples(int total = Total)
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

        Messages.PrintResultado("Insert Simples EF (row-by-row)", total, sw.Elapsed);
    }
}