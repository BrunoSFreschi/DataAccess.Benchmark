using Microsoft.Data.Sqlite;

namespace DataAccess.Benchmark;

public class DbFactory
{
    public const string ConnectionString = "Data Source=benchmark.db";

    public static SqliteConnection Create()
        => new SqliteConnection(ConnectionString);

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
            );
        ";
        command.ExecuteNonQuery();
    }
}