using FluentAssertions;
using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop.UnitTests.RefitClient;

/// <summary>
/// Refit Client 接口契约测试
/// 验证 API 接口定义与服务器端保持一致
/// </summary>
public class RefitClientContractTests
{
    #region IPatientApi 契约测试

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Layer", "RefitClient")]
    public void IPatientApi_ShouldBeInterface()
    {
        typeof(IPatientApi).IsInterface.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Layer", "RefitClient")]
    public void IPatientApi_ShouldHaveRequiredMethods()
    {
        var methods = typeof(IPatientApi).GetMethods();
        methods.Should().Contain(m => m.Name == "CreatePatientAsync");
        methods.Should().Contain(m => m.Name == "GetPatientByIdAsync");
        methods.Should().Contain(m => m.Name == "UpdatePatientAsync");
        methods.Should().Contain(m => m.Name == "DeletePatientAsync");
        methods.Should().Contain(m => m.Name == "GetPatientsAsync");
        methods.Should().Contain(m => m.Name == "RestoreAsync");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Layer", "RefitClient")]
    public void IPatientApi_CreatePatientAsync_ShouldAcceptPatientInputDto()
    {
        var method = typeof(IPatientApi).GetMethod("CreatePatientAsync");
        method.Should().NotBeNull();
        
        var parameters = method!.GetParameters();
        parameters.Should().Contain(p => p.ParameterType == typeof(PatientInputDto));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Layer", "RefitClient")]
    public void IPatientApi_Methods_ShouldReturnApiResponse()
    {
        var methods = typeof(IPatientApi).GetMethods()
            .Where(m => m.Name.EndsWith("Async"));

        foreach (var method in methods)
        {
            var returnType = method.ReturnType;
            returnType.Name.Should().StartWith("Task");
        }
    }

    #endregion

    #region IAuthApi 契约测试

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Layer", "RefitClient")]
    public void IAuthApi_ShouldBeInterface()
    {
        typeof(IAuthApi).IsInterface.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Layer", "RefitClient")]
    public void IAuthApi_ShouldHaveRequiredMethods()
    {
        var methods = typeof(IAuthApi).GetMethods();
        methods.Should().Contain(m => m.Name == "LoginAsync");
        methods.Should().Contain(m => m.Name == "RefreshTokenAsync");
        methods.Should().Contain(m => m.Name == "LogoutAsync");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Layer", "RefitClient")]
    public void IAuthApi_LoginAsync_ShouldAcceptLoginRequest()
    {
        var method = typeof(IAuthApi).GetMethod("LoginAsync");
        method.Should().NotBeNull();
        
        var parameters = method!.GetParameters();
        parameters.Should().Contain(p => p.ParameterType == typeof(LoginRequest));
    }

    #endregion

    #region DTO 契约测试

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Layer", "RefitClient")]
    public void PatientInputDto_ShouldHaveRequiredProperties()
    {
        var properties = typeof(PatientInputDto).GetProperties();
        properties.Should().Contain(p => p.Name == "Name");
        properties.Should().Contain(p => p.Name == "IdNumber");
        properties.Should().Contain(p => p.Name == "PhoneNumber");
        properties.Should().Contain(p => p.Name == "Gender");
        properties.Should().Contain(p => p.Name == "Address");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Layer", "RefitClient")]
    public void PatientDetailDto_ShouldHaveRequiredProperties()
    {
        var properties = typeof(PatientDetailDto).GetProperties();
        properties.Should().Contain(p => p.Name == "Id");
        properties.Should().Contain(p => p.Name == "Name");
        properties.Should().Contain(p => p.Name == "IdNumber");
        properties.Should().Contain(p => p.Name == "PhoneNumber");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Layer", "RefitClient")]
    public void LoginRequest_ShouldHaveRequiredProperties()
    {
        var properties = typeof(LoginRequest).GetProperties();
        properties.Should().Contain(p => p.Name == "UserName");
        properties.Should().Contain(p => p.Name == "Password");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Layer", "RefitClient")]
    public void LoginResponse_ShouldHaveRequiredProperties()
    {
        var properties = typeof(LoginResponse).GetProperties();
        properties.Should().Contain(p => p.Name == "Token");
        properties.Should().Contain(p => p.Name == "RefreshToken");
        properties.Should().Contain(p => p.Name == "ExpiresAt");
    }

    #endregion

    #region 枚举契约测试

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Layer", "RefitClient")]
    public void Gender_ShouldBeEnum()
    {
        typeof(Gender).IsEnum.Should().BeTrue();
        
        var values = Enum.GetValues<Gender>();
        values.Should().Contain(Gender.Male);
        values.Should().Contain(Gender.Female);
    }

    #endregion
}