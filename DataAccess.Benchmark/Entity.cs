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
        throw new NotImplementedException();
    }
}