using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace LYBT.Tests.Architecture;

/// <summary>
/// Testing Trophy 防护规则: 确保 Server 测试项目不使用 mock 框架。
/// Desktop 测试允许使用 NSubstitute (仅限 WPF 边界接口白名单)。
/// </summary>
public sealed class AntiMockRuleTests
{
    private static Assembly ServerTestAssembly =>
        typeof(Server.Infrastructure.ServerFixture).Assembly;

    [Fact]
    public void ServerTestProject_ShouldNotReference_NSubstitute()
    {
        var referencedAssemblies = ServerTestAssembly.GetReferencedAssemblies();

        referencedAssemblies
            .Should().NotContain(
                a => a.Name == "NSubstitute",
                "Server tests must not use mocks - use real HTTP integration tests instead (Testing Trophy)");
    }

    [Fact]
    public void ServerTestProject_ShouldNotContain_TypesDependingOnNSubstitute()
    {
        var types = Types.InAssembly(ServerTestAssembly)
            .That().HaveDependencyOn("NSubstitute")
            .GetTypes();

        types.Should().BeEmpty(
            "No class in LYBT.Tests.Server should reference NSubstitute - " +
            "all server tests use real database and HTTP pipeline");
    }

    [Fact]
    public void ServerTestProject_ShouldNotReference_EFCoreInMemory_ForIntegrationTests()
    {
        // Integration tests (inheriting IntegrationTestBase<T>) should use real SQL Server via Respawn,
        // not EF Core InMemory. InMemory is only allowed for pure logic tests that need a quick DbContext.
        // Get all types that inherit from IntegrationTestBase<> (generic base class)
        var allTypes = Types.InAssembly(ServerTestAssembly).GetTypes();
        var integrationTestTypes = allTypes
            .Where(t => t.BaseType != null &&
                        t.BaseType.IsGenericType &&
                        t.BaseType.GetGenericTypeDefinition() == typeof(Server.Infrastructure.IntegrationTestBase<>))
            .ToList();

        foreach (var testType in integrationTestTypes)
        {
            var hasDependency = Types.InAssembly(ServerTestAssembly)
                .That().HaveName(testType.Name)
                .And().HaveDependencyOn("Microsoft.EntityFrameworkCore.InMemory")
                .GetTypes();

            hasDependency.Should().BeEmpty(
                $"Integration test '{testType.Name}' should not use EF Core InMemory - " +
                "use real SQL Server via ServerFixture instead");
        }
    }
}
