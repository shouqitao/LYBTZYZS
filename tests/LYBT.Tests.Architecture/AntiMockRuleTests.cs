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
        typeof(LYBT.Tests.Server.DatabaseInitializationServiceTests).Assembly;

    [Fact]
    public void AM01_ServerTests_No_NSubstitute_Reference()
    {
        var referencedAssemblies = ServerTestAssembly.GetReferencedAssemblies();

        referencedAssemblies
            .Should().NotContain(
                a => a.Name == "NSubstitute",
                "Server tests must not use mocks - use real HTTP integration tests instead (Testing Trophy)");
    }

    [Fact]
    public void AM02_ServerTests_No_NSubstitute_Dependencies()
    {
        var types = Types.InAssembly(ServerTestAssembly)
            .That().HaveDependencyOn("NSubstitute")
            .GetTypes();

        types.Should().BeEmpty(
            "No class in LYBT.Tests.Server should reference NSubstitute - " +
            "all server tests use real database and HTTP pipeline");
    }
}
