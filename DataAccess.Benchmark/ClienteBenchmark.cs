using BenchmarkDotNet.Attributes;

namespace DataAccess.Benchmark;

[MemoryDiagnoser]
public class ClienteBenchmark
{
    [GlobalSetup]
    internal static void AdonetExec()
    {
        DbFactory.ResetDatabase();

        DbFactory.InicializarBanco();

        Console.WriteLine("===== BENCHMARK DE INSERTS SQLITE =====\n");

        Adonet.InsertSimples();
        Adonet.InsertBatch();
        Adonet.InsertParalelo();

        Console.WriteLine("\n✓ Testes concluídos!");
    }

    internal static void DapperExec()
    {
        DbFactory.ResetDatabase();

        DbFactory.InicializarBanco();

        Console.WriteLine("===== BENCHMARK DE INSERTS SQLITE =====\n");

        Dapper.InsertSimples();

        throw new NotImplementedException();
    }
}