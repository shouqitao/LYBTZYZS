using FluentAssertions;
using LYBT.Desktop.Contracts.Results;
using Xunit;

namespace LYBT.Tests.Desktop;

public class CommandResultTests
{
    [Fact]
    public void Succeeded_Should_When_DataProvided_ReturnsSuccessTrue()
    {
        var data = new { Name = "test" };
        var result = CommandResult<object>.Succeeded(data);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(data);
        result.Error.Should().BeNull();
        ((bool)result).Should().BeTrue();
    }

    [Fact]
    public void Failed_Should_When_ErrorProvided_ReturnsSuccessFalse()
    {
        var result = CommandResult<string>.Failed("业务失败");

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().Be("业务失败");
        ((bool)result).Should().BeFalse();
    }

    [Fact]
    public void NotFound_Should_When_Called_ReturnsFailedWithNotFoundMessage()
    {
        var result = CommandResult<int>.NotFound();
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("未找到");
    }

    [Fact]
    public void Failed_WithGuid_Should_When_CreateFails_ReturnsError()
    {
        var result = CommandResult<Guid>.Failed("创建医案失败");
        result.Success.Should().BeFalse();
        result.Data.Should().Be(Guid.Empty);
        result.Error.Should().Be("创建医案失败");
    }

    [Fact]
    public void Succeeded_Bool_Should_When_SaveSucceeds_ReturnsTrue()
    {
        var result = CommandResult<bool>.Succeeded(true);
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public void NonGeneric_Succeeded_Should_When_NoData_ReturnsSuccess()
    {
        var result = CommandResult.Succeeded();
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        ((bool)result).Should().BeTrue();
    }

    [Fact]
    public void NonGeneric_Failed_Should_When_Error_ReturnsSuccessFalse()
    {
        var result = CommandResult.Failed("操作失败");
        result.Success.Should().BeFalse();
        result.Error.Should().Be("操作失败");
    }
}
