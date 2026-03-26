using Microsoft.Data.Sqlite;
using System.Data.SQLite;

namespace DataAccess.Benchmark;

public class DbFactory
{
    public const string ConnectionString = "Data Source=benchmark.db";

    public static SqliteConnection Create()
        => new(ConnectionString);

    public static void ResetDatabase()
    {
        if (File.Exists("benchmark.db"))
            File.Delete("benchmark.db");

        using var connection = Create();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            DROP TABLE IF EXISTS Pessoas;
            CREATE TABLE Pessoas (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome TEXT NOT NULL,
                Email TEXT NOT NULL,
                Ativo INTEGER NOT NULL,
                DataCriacao TEXT NOT NULL
            );";
        command.ExecuteNonQuery();
    }

    // Inicializa banco com WAL ativado
    internal static void InicializarBanco()
    {
        using var conn = new SQLiteConnection(ConnectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = "PRAGMA journal_mode=WAL;";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "PRAGMA synchronous=NORMAL;";
        cmd.ExecuteNonQuery();

        Console.WriteLine("- Banco inicializado com WAL e otimizações");
    }
}