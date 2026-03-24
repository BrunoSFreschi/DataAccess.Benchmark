using BenchmarkDotNet.Attributes;

namespace DataAccess.Benchmark;

[MemoryDiagnoser]
public class ClienteBenchmark
{
    [GlobalSetup]
    public async void Setup()
    {
        DbFactory.ResetDatabase();

    
        Console.WriteLine("===== BENCHMARK DE INSERTS SQLITE =====\n");

        // Inicializar com WAL
        Adonet.InicializarBanco();

        // Teste 1: Simples (1M)
        Adonet.InsertClientes();

        // Teste 2: Batch (1M)
        Adonet.InsertBatch();

        // Teste 3: Paralelo (sem limpar, só para comparar)
        await Adonet.InsertParaleloAsync();

        Console.WriteLine("\n✓ Testes concluídos!");
    }
}