using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Casino.Tests;

[TestClass]
public sealed class TestDatabaseFixture
{
    private static string? _databaseName;
    private static string? _adminConnectionString;

    public static string ConnectionString { get; private set; } = string.Empty;

    [AssemblyInitialize]
    public static async Task CreateIsolatedDatabase(TestContext _)
    {
        var settingsPath = FindAppSettings();
        using var settings = JsonDocument.Parse(await File.ReadAllTextAsync(settingsPath));
        var mainConnection = settings.RootElement.GetProperty("ConnectionStrings")
            .GetProperty("DefaultConnection").GetString()
            ?? throw new InvalidOperationException("No se encontró ConnectionStrings:DefaultConnection.");

        var adminBuilder = new NpgsqlConnectionStringBuilder(mainConnection)
        {
            Database = "postgres",
            Pooling = false
        };
        if (!new[] { "localhost", "127.0.0.1", "::1" }.Contains(adminBuilder.Host, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Las pruebas solo crean bases temporales en PostgreSQL local para evitar tocar un servidor remoto.");
        _adminConnectionString = adminBuilder.ConnectionString;
        _databaseName = $"casino_test_{Guid.NewGuid():N}";

        await using (var connection = new NpgsqlConnection(_adminConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
            await command.ExecuteNonQueryAsync();
        }

        var testBuilder = new NpgsqlConnectionStringBuilder(mainConnection)
        {
            Database = _databaseName,
            Pooling = false
        };
        ConnectionString = testBuilder.ConnectionString;
    }

    [AssemblyCleanup]
    public static async Task DropIsolatedDatabase()
    {
        if (_databaseName is null || _adminConnectionString is null) return;

        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using (var terminate = connection.CreateCommand())
        {
            terminate.CommandText = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database AND pid <> pg_backend_pid()";
            terminate.Parameters.AddWithValue("database", _databaseName);
            await terminate.ExecuteNonQueryAsync();
        }
        await using (var drop = connection.CreateCommand())
        {
            drop.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\"";
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static string FindAppSettings()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "Casino.API", "appsettings.json");
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException("No se encontró Casino.API/appsettings.json para conectar con PostgreSQL local.");
    }
}
