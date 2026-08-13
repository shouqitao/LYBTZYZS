using FluentAssertions;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// ControllerBaseExtensions.HandleResult 状态码映射守卫（PATIENT-PHONE-409-FIX:
/// Result.Failure(ErrorCode) 的 ModuleErrorCode 为空时必须回退 ErrorCode 映射——
/// 原只认 ModuleErrorCode → 电话唯一等 409 语义落入 BusinessFail 恒 422）
/// </summary>
public class ControllerBaseExtensionsTests
{
    [Fact]
    public void HandleResult_WhenErrorCodeIsPhoneDuplicate_Returns409Not422()
    {
        var controller = new TestController();
        var result = Result<object>.Failure(ErrorCode.PatientPhoneDuplicate, "该手机号已关联其他患者");

        var actionResult = ControllerBaseExtensions.HandleResult(controller, result, useAuthMapping: true);

        actionResult.Should().BeOfType<ObjectResult>("409 走 StatusCode(409, response) 路径");
        var objectResult = (ObjectResult)actionResult;
        objectResult.StatusCode.Should().Be(409, "电话唯一冲突必须返回 409（US-PAT-003/004），而非 BusinessFail 的 422");
    }

    [Fact]
    public void HandleResult_WhenModuleErrorCodeNull_FallsBackToErrorCode()
    {
        // Result.Failure(ErrorCode) 不设 ModuleErrorCode——HandleResult 必须回退 ErrorCode
        var controller = new TestController();
        var result = Result<object>.Failure(ErrorCode.NotFound, "资源不存在");

        var actionResult = ControllerBaseExtensions.HandleResult(controller, result, useAuthMapping: true);

        // NotFoundObjectResult/ObjectResult 均有 StatusCode（继承 ObjectResult）——按属性断言
        actionResult.Should().BeAssignableTo<ObjectResult>();
        ((ObjectResult)actionResult).StatusCode.Should().Be(404, "ErrorCode.NotFound → 404（回退映射生效）");
    }

    [Fact]
    public void HandleResult_WhenForbidden_Returns403()
    {
        var controller = new TestController();
        var result = Result<object>.Failure(ErrorCode.Forbidden, "权限不足");

        var actionResult = ControllerBaseExtensions.HandleResult(controller, result, useAuthMapping: true);

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(403, "ErrorCode.Forbidden → 403（回退映射生效）");
    }

    private sealed class TestController : ControllerBase
    {
        public TestController()
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }
    }
}
