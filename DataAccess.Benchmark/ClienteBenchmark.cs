namespace DataAccess.Benchmark;

public class ClienteBenchmark
{
    internal static void AdonetExec()
    {
        DbFactory.ResetDatabase();

        DbFactory.InicializarBanco();

        Console.WriteLine("===== BENCHMARK DE INSERTS ADO Net =====\n");

        Adonet.InsertSimples();
        Adonet.InsertBatch();
        Adonet.InsertParalelo();

        Console.WriteLine("\n✓ Testes concluídos!");
    }

    internal static void DapperExec()
    {
        DbFactory.ResetDatabase();

        DbFactory.InicializarBanco();

        Console.WriteLine("===== BENCHMARK DE INSERTS Dapper =====\n");

        Dapper.InsertSimples();
        Dapper.InsertBatch();
        Dapper.InsertParalelo();

        Console.WriteLine("\n✓ Testes concluídos!");
    }

    internal static void EntityExec()
    {
        DbFactory.ResetDatabase();

        DbFactory.InicializarBanco();

        Console.WriteLine("===== BENCHMARK DE INSERTS  Entity =====\n");

        Entity.InsertSimples();
        Entity.InsertBatch();
        Entity.InsertParalelo();

        Console.WriteLine("\n✓ Testes concluídos!");
    }
}