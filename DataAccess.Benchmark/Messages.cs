namespace DataAccess.Benchmark;

public class Messages
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
}
