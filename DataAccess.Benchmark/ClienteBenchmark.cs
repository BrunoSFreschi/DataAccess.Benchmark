using BenchmarkDotNet.Attributes;

namespace DataAccess.Benchmark;

[MemoryDiagnoser]
public class ClienteBenchmark
{
    [GlobalSetup]
    public static void Setup()
    {
        DbFactory.ResetDatabase();

        DbFactory.InicializarBanco();


        Console.WriteLine("===== BENCHMARK DE INSERTS SQLITE =====\n");

        Adonet.InsertSimples();
        Adonet.InsertBatch();
        Adonet.InsertParalelo();

        Console.WriteLine("\n✓ Testes concluídos!");
    }
}