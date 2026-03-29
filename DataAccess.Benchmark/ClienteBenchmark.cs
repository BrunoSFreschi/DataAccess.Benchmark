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

        throw new NotImplementedException();
    }
}