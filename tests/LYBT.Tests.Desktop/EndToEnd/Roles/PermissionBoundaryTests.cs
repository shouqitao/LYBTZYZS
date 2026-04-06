using FluentAssertions;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Roles;

public class PermissionBoundaryTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public PermissionBoundaryTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Role", "SuperAdmin")]
    [Trait("Phase", "Permission")]
    public async Task SuperAdmin_CanAccessUserManagement()
    {
        await LoginAsSysadminAsync();
        
        var response = await UserApi.GetUsersAsync();
        
        response.Success.Should().BeTrue("SuperAdmin should have access to user management");
        response.Data.Should().NotBeNull();
        _output.WriteLine($"SuperAdmin can access users: {response.Data!.Items.Count} users found");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Role", "SuperAdmin")]
    [Trait("Phase", "Permission")]
    public async Task SuperAdmin_CanAccessSystemDiagnostics()
    {
        await LoginAsSysadminAsync();
        
        var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/v1/diagnostics/logging/status");
        
        response.IsSuccessStatusCode.Should().BeTrue("SuperAdmin should have access to diagnostics");
        _output.WriteLine($"SuperAdmin can access diagnostics: {response.StatusCode}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Role", "SuperAdmin")]
    [Trait("Phase", "Permission")]
    public async Task SuperAdmin_CanAccessAllMedicalCases()
    {
        await LoginAsSysadminAsync();
        
        var response = await MedicalCaseApi.GetMedicalCasesAsync();
        
        response.Success.Should().BeTrue("SuperAdmin should have access to all medical cases");
        _output.WriteLine($"SuperAdmin can access medical cases: {response.Data?.Items.Count ?? 0} cases found");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Role", "SuperAdmin")]
    [Trait("Phase", "Permission")]
    public async Task SuperAdmin_CanAccessHealthDetails()
    {
        await LoginAsSysadminAsync();
        
        var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/v1/health/details");
        
        response.IsSuccessStatusCode.Should().BeTrue("SuperAdmin should have access to health details");
        _output.WriteLine($"SuperAdmin can access health details: {response.StatusCode}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Role", "Permission")]
    [Trait("Phase", "Boundary")]
    public async Task AnonymousUser_CanAccessBasicHealthCheck()
    {
        var client = new System.Net.Http.HttpClient
        {
            BaseAddress = new System.Uri(GetBaseUrl())
        };
        
        var response = await client.GetAsync("/api/v1/health");
        
        response.IsSuccessStatusCode.Should().BeTrue("Anonymous users should have access to basic health check");
        _output.WriteLine($"Anonymous user can access health check: {response.StatusCode}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Role", "Permission")]
    [Trait("Phase", "Boundary")]
    public async Task AnonymousUser_CannotAccessUserManagement()
    {
        var client = new System.Net.Http.HttpClient
        {
            BaseAddress = new System.Uri(GetBaseUrl())
        };
        
        var response = await client.GetAsync("/api/v1/users");
        
        response.IsSuccessStatusCode.Should().BeFalse("Anonymous users should NOT have access to user management");
        _output.WriteLine($"Anonymous user correctly denied user management: {response.StatusCode}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Role", "Permission")]
    [Trait("Phase", "Boundary")]
    public async Task AnonymousUser_CannotAccessMedicalCases()
    {
        var client = new System.Net.Http.HttpClient
        {
            BaseAddress = new System.Uri(GetBaseUrl())
        };
        
        var response = await client.GetAsync("/api/v1/medicalcases");
        
        response.IsSuccessStatusCode.Should().BeFalse("Anonymous users should NOT have access to medical cases");
        _output.WriteLine($"Anonymous user correctly denied medical cases: {response.StatusCode}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Role", "Permission")]
    [Trait("Phase", "Boundary")]
    public async Task AnonymousUser_CannotAccessDiagnostics()
    {
        var client = new System.Net.Http.HttpClient
        {
            BaseAddress = new System.Uri(GetBaseUrl())
        };
        
        var response = await client.GetAsync("/api/v1/diagnostics/logging/status");
        
        response.IsSuccessStatusCode.Should().BeFalse("Anonymous users should NOT have access to diagnostics");
        _output.WriteLine($"Anonymous user correctly denied diagnostics: {response.StatusCode}");
    }
}
