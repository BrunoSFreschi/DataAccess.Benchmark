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

        // Teste 1: Simples (1M)
        Adonet.InsertClientes();

        // Teste 2: Batch (1M)
        Adonet.InsertBatch();

        // Teste 3: Paralelo (sem limpar, só para comparar)
        Adonet.InsertParaleloSimples();

        Console.WriteLine("\n✓ Testes concluídos!");
    }
}