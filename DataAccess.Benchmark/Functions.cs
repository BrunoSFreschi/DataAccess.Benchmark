namespace DataAccess.Benchmark;

public class Functions
{
    public static void PrintResultado(string descricao, int total, TimeSpan tempo)
    {
        Console.WriteLine();
        Console.WriteLine($"[{descricao}]");
        Console.WriteLine($"Registros: {total:N0}");
        Console.WriteLine($"Tempo: {tempo.TotalSeconds:F2}s");
        Console.WriteLine($"Taxa: {total / tempo.TotalSeconds:N0} registros/s");
        Console.WriteLine();
    }

    public static class BenchmarkConfig
    {
        public const string ConnectionString = "Data Source=benchmark.db;";
        public const int Total = 1_000_000;
        public const int BatchSize = 500;
    }
}