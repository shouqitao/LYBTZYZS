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

    public string ConnectionString => GetFullConnectionString();

    private string GetBaseConnectionString()
    {
        // Check for external SQL Server connection string from environment
        var envConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrEmpty(envConnectionString))
        {
            // Parse and extract base connection, removing only Database parameter
            var builder = new SqlConnectionStringBuilder(envConnectionString);
            builder.Remove("Database"); // Remove any existing database
            return builder.ConnectionString;
        }

        // Fall back to default LocalDB
        return MasterConnectionString;
    }

    private string GetFullConnectionString()
    {
        var baseConnectionString = GetBaseConnectionString();
        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            ["Database"] = _databaseName,
            ["Encrypt"] = true,  // Enable SSL for external SQL Server
            ["TrustServerCertificate"] = true  // Trust server certificate to avoid validation errors
        };
        return builder.ConnectionString;
    }

    public async Task InitializeAsync()
    {
        // Get base connection string from environment or use default
        var baseConnectionString = GetBaseConnectionString();

        await using var connection = new SqlConnection(baseConnectionString);
        await connection.OpenAsync();

        var sql = $"CREATE DATABASE [{_databaseName}]";
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            var baseConnectionString = GetBaseConnectionString();
            await using var connection = new SqlConnection(baseConnectionString);
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
