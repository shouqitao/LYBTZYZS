using Microsoft.EntityFrameworkCore;

namespace LYBT.Tests.Server._Infrastructure;

public static class TestDbFactory
{
    public static DbContextOptions<T> CreateOptions<T>(string connectionString) where T : DbContext
    {
        return new DbContextOptionsBuilder<T>()
            .UseSqlServer(connectionString)
            .Options;
    }

    public static string GetTestConnectionString()
    {
        // 优先环境变量，回退 LocalDB
        return Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING")
            ?? @"Server=(localdb)\MSSQLLocalDB;Database=LYBT_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
    }
}
