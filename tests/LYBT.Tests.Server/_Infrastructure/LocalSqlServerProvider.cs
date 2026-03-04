using Microsoft.Data.SqlClient;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Creates a unique SQL Server test database per fixture lifetime.
/// Database name: LYBT_Test_{timestamp}_{guid8chars}
/// Drops the database on dispose.
/// </summary>
public sealed class LocalSqlServerProvider : ITestDatabaseProvider
{
    private const string MasterConnectionString =
        "Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True";

    private readonly string _databaseName;

    public LocalSqlServerProvider()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var guidPart = Guid.NewGuid().ToString("N")[..8];
        _databaseName = $"LYBT_Test_{timestamp}_{guidPart}";
    }

    public string ConnectionString =>
        $"Server=localhost;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    public async Task InitializeAsync()
    {
        await using var connection = new SqlConnection(MasterConnectionString);
        await connection.OpenAsync();

        var sql = $"CREATE DATABASE [{_databaseName}]";
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await using var connection = new SqlConnection(MasterConnectionString);
            await connection.OpenAsync();

            // Force disconnect all users, then drop
            var sql = $"""
                IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '{_databaseName}')
                BEGIN
                    ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{_databaseName}];
                END
                """;
            await using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException)
        {
            // Best-effort cleanup; don't fail test teardown
        }
    }
}
