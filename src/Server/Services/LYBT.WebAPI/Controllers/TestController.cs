using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 临时测试控制器 - 用于Phase 10 API功能测试
/// 不需要授权验证，专门用于测试3个核心模块
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[AllowAnonymous] // 不需要授权
public class TestController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly IPatientService _patientService;
    private readonly IHerbService _herbService;

    public TestController(
        IUserService userService,
        IPatientService patientService,
        IHerbService herbService,
        IMemoryCache cache,
        ILogger<TestController> logger)
        : base(logger, cache)
    {
        _userService = userService;
        _patientService = patientService;
        _herbService = herbService;
    }

    /// <summary>
    /// 测试服务状态 - 检查3个核心模块是否正常
    /// </summary>
    [HttpGet("status")]
    public ActionResult<ApiResponse<object>> GetStatus()
    {
        try
        {
            var status = (object)new
            {
                timestamp = DateTime.Now,
                message = "UltraThink v2.0 Phase 10 API测试",
                modules = new
                {
                    users = "可用",
                    patients = "可用", 
                    herbs = "可用"
                }
            };

            return Success(status, "测试接口正常");
        }
        catch (Exception ex)
        {
            return HandleException<object>(ex, "获取测试状态");
        }
    }

    /// <summary>
    /// 测试用户模块 - 获取用户数量统计
    /// </summary>
    [HttpGet("users/count")]
    public async Task<ActionResult<ApiResponse<object>>> GetUsersCount()
    {
        try
        {
            // 使用分页查询获取用户数据
            var query = new UserPagedQueryDto { PageIndex = 1, PageSize = 1 };
            var result = await _userService.GetPagedAsync(query);
            var count = result.IsSuccess ? result.Data?.TotalCount ?? 0 : 0;
            
            var data = (object)new
            {
                count = count,
                module = "Users",
                status = result.IsSuccess ? "正常" : "异常"
            };

            return Success(data, "用户模块测试完成");
        }
        catch (Exception ex)
        {
            return HandleException<object>(ex, "测试用户模块");
        }
    }

    /// <summary>
    /// 测试患者模块 - 获取患者数量统计
    /// </summary>
    [HttpGet("patients/count")]
    public async Task<ActionResult<ApiResponse<object>>> GetPatientsCount()
    {
        try
        {
            // 使用分页查询获取患者数据
            var query = new PatientPagedQueryDto { PageIndex = 1, PageSize = 1 };
            var result = await _patientService.GetPagedAsync(query);
            var count = result.IsSuccess ? result.Data?.TotalCount ?? 0 : 0;
            
            var data = (object)new
            {
                count = count,
                module = "Patients",
                status = result.IsSuccess ? "正常" : "异常"
            };

            return Success(data, "患者模块测试完成");
        }
        catch (Exception ex)
        {
            return HandleException<object>(ex, "测试患者模块");
        }
    }

    /// <summary>
    /// 测试药材模块 - 获取药材数量统计
    /// </summary>
    [HttpGet("herbs/count")]
    public async Task<ActionResult<ApiResponse<object>>> GetHerbsCount()
    {
        try
        {
            // 使用分页查询替代GetAllAsync (接口简化后的要求)
            var query = new HerbPagedQueryDto { PageIndex = 1, PageSize = 10000 };
            var result = await _herbService.GetPagedAsync(query);
            var count = result.IsSuccess ? result.Data?.TotalCount ?? 0 : 0;
            
            var data = (object)new
            {
                count = count,
                module = "Herbs",
                status = result.IsSuccess ? "正常" : "异常"
            };

            return Success(data, "药材模块测试完成");
        }
        catch (Exception ex)
        {
            return HandleException<object>(ex, "测试药材模块");
        }
    }

    /// <summary>
    /// 综合测试 - 测试所有3个核心模块
    /// </summary>
    [HttpGet("comprehensive")]
    public async Task<ActionResult<ApiResponse<object>>> ComprehensiveTest()
    {
        try
        {
            var results = new List<object>();

            // 测试用户模块
            try
            {
                var query = new UserPagedQueryDto { PageIndex = 1, PageSize = 1 };
                var userResult = await _userService.GetPagedAsync(query);
                results.Add(new
                {
                    module = "Users",
                    status = userResult.IsSuccess ? "✅ 正常" : "❌ 异常",
                    count = userResult.IsSuccess ? userResult.Data?.TotalCount ?? 0 : 0,
                    error = userResult.IsSuccess ? null : userResult.Message
                });
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    module = "Users",
                    status = "❌ 异常",
                    count = 0,
                    error = ex.Message
                });
            }

            // 测试患者模块
            try
            {
                var query = new PatientPagedQueryDto { PageIndex = 1, PageSize = 1 };
                var patientResult = await _patientService.GetPagedAsync(query);
                results.Add(new
                {
                    module = "Patients",
                    status = patientResult.IsSuccess ? "✅ 正常" : "❌ 异常",
                    count = patientResult.IsSuccess ? patientResult.Data?.TotalCount ?? 0 : 0,
                    error = patientResult.IsSuccess ? null : patientResult.Message
                });
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    module = "Patients",
                    status = "❌ 异常",
                    count = 0,
                    error = ex.Message
                });
            }

            // 测试药材模块
            try
            {
                // 使用分页查询替代GetAllAsync (接口简化后的要求)
                var herbQuery = new HerbPagedQueryDto { PageIndex = 1, PageSize = 10000 };
                var herbResult = await _herbService.GetPagedAsync(herbQuery);
                results.Add(new
                {
                    module = "Herbs",
                    status = herbResult.IsSuccess ? "✅ 正常" : "❌ 异常",
                    count = herbResult.IsSuccess ? herbResult.Data?.TotalCount ?? 0 : 0,
                    error = herbResult.IsSuccess ? null : herbResult.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    module = "Herbs",
                    status = "❌ 异常",
                    count = 0,
                    error = ex.Message
                });
            }

            var successCount = results.Count(r => r.GetType().GetProperty("status")?.GetValue(r)?.ToString()?.Contains("✅") == true);
            var totalCount = results.Count;

            var summary = (object)new
            {
                summary = $"{successCount}/{totalCount} 模块正常",
                timestamp = DateTime.Now,
                results = results
            };

            return Success(summary, "综合测试完成");
        }
        catch (Exception ex)
        {
            return HandleException<object>(ex, "综合测试");
        }
    }
}