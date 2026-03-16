using BenchmarkDotNet.Attributes;

namespace DataAccess.Benchmark;

[MemoryDiagnoser]
public class ClienteBenchmark
{
    [GlobalSetup]
    public void Setup()
    {
        DbFactory.ResetDatabase();
    }
}