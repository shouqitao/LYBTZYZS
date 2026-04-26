using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using LYBT.Entities;
using LYBT.Entities.Enums;
using LYBT.LocalWebAPI.Auth;
using LYBT.LocalWebAPI.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// LocalWebAPI DbContext tests - SQLite InMemory
/// </summary>
public class LocalWebApiDbContextTests
{
    private static SqliteConnection CreateInMemoryConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static LocalWebApiDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<LocalWebApiDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new LocalWebApiDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task Can_Create_And_Query_Patients()
    {
        await using var connection = CreateInMemoryConnection();
        await using var context = CreateContext(connection);

        var patient = new Patient { Name = "Test Patient", Gender = Gender.Male };
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var found = await context.Patients.FirstOrDefaultAsync(p => p.Name == "Test Patient");
        found.Should().NotBeNull();
        found!.Name.Should().Be("Test Patient");
    }

    [Fact]
    public async Task Soft_Delete_Filters_Work()
    {
        await using var connection = CreateInMemoryConnection();
        await using var context = CreateContext(connection);

        var herb = new Herb { Name = "Test Herb", IsDeleted = false };
        context.Herbs.Add(herb);
        await context.SaveChangesAsync();

        // Soft delete
        herb.IsDeleted = true;
        await context.SaveChangesAsync();

        // Query should not find deleted items (if global query filter is applied)
        var notDeleted = await context.Herbs.FirstOrDefaultAsync(h => !h.IsDeleted && h.Name == "Test Herb");
        notDeleted.Should().BeNull();

        // With IgnoreQueryFilters, should find it
        var withDeleted = await context.Herbs.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Name == "Test Herb");
        withDeleted.Should().NotBeNull();
    }

    [Fact]
    public async Task SeedData_Creates_Admin_User()
    {
        await using var connection = CreateInMemoryConnection();
        await using var context = CreateContext(connection);

        await LocalWebApiSeedData.SeedAsync(context);

        var admin = await context.Users.FirstOrDefaultAsync(u => u.UserName == "admin");
        admin.Should().NotBeNull();
        admin!.Role.Should().Be(UserRole.Admin);
    }
}

/// <summary>
/// LocalJwtConfig tests
/// </summary>
public class LocalJwtConfigTests
{
    [Fact]
    public void GenerateToken_Produces_Valid_Jwt()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "testuser", Role = UserRole.Admin };
        var token = LocalJwtConfig.GenerateToken(user);

        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Subject.Should().Be(user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddDays(365), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateToken_Contains_Sub_Claim()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "testuser", Role = UserRole.Clinical };
        var token = LocalJwtConfig.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == "sub");
    }
}
