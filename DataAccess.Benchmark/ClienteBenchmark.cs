using BenchmarkDotNet.Attributes;

namespace DataAccess.Benchmark;

[MemoryDiagnoser]
public class ClienteBenchmark
{
    [GlobalSetup]
    public void Setup()
    {
        DbFactory.ResetDatabase();

        Adonet.InsertClientes();

        Console.WriteLine("=== Sequencial 1M ===");
        Adonet.InsertClientes(1_000_000);

        Console.WriteLine("\n=== Paralelo 4 threads x 250k ===");
        Adonet.InsertParaleloAsync(threads: 4, porThread: 250_000);

        Console.WriteLine("\n=== Batch INSERT multi-row ===");
        Adonet.InsertBatch(1_000_000, batchSize: 500);
    }
}