# UltraThink控制器开发模板 - 标准化代码模板

**文档版本**: v1.0  
**创建日期**: 2025-08-17  
**最后更新**: 2025-08-17  
**架构师**: UltraThink AI System  

## 📋 概述

本文档提供了LYBT系统中各类控制器的标准化代码模板，帮助开发者快速创建符合UltraThink架构标准的控制器。

## 🎯 模板分类

### 1. 业务API控制器模板
### 2. 系统管理控制器模板  
### 3. 导入导出控制器模板
### 4. 查询专用控制器模板

---

## 🏢 业务API控制器模板

### 标准CRUD业务控制器

```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.{ModuleName}; // 替换为具体模块名
using LYBT.Module.{ModuleName}.Interfaces;        // 替换为具体模块名

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// {业务名称}管理控制器 - UltraThink标准架构
    /// 提供{业务名称}的CRUD操作和业务功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class {EntityName}Controller : BaseApiController
    {
        private readonly I{EntityName}Service _{entityName}Service;

        /// <summary>
        /// 构造方法 - 注入{业务名称}服务
        /// </summary>
        public {EntityName}Controller(
            I{EntityName}Service {entityName}Service, 
            ILogger<{EntityName}Controller> logger,
            IMemoryCache cache)
            : base(logger, cache)
        {
            _{entityName}Service = {entityName}Service;
        }

        #region 基础CRUD操作

        /// <summary>
        /// 分页查询{业务名称} - 标准分页接口
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<PagedApiResponse<{EntityName}Dto>>> GetPaged([FromBody] {EntityName}QueryDto query)
        {
            try
            {
                var validation = ValidateModel<{EntityName}Dto>();
                if (validation != null) return validation;

                var result = await _{entityName}Service.GetPagedAsync(query);
                return HandlePagedServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<{EntityName}Dto>(ex, "分页查询{业务名称}", query);
            }
        }

        /// <summary>
        /// 根据ID获取{业务名称}详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<{EntityName}Dto>>> GetById(Guid id)
        {
            try
            {
                var validation = ValidateGuid<{EntityName}Dto>(id, "{业务名称}ID");
                if (validation != null) return validation;

                var result = await _{entityName}Service.GetByIdAsync(id);
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<{EntityName}Dto>(ex, "获取{业务名称}详情", id);
            }
        }

        /// <summary>
        /// 创建{业务名称}
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<{EntityName}Dto>>> Create([FromBody] {EntityName}CreateDto dto)
        {
            try
            {
                var validation = ValidateModel<{EntityName}Dto>();
                if (validation != null) return validation;

                var result = await _{entityName}Service.CreateAsync(dto);
                
                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("创建{业务名称}", dto, result.Data.Id);
                }
                
                return HandleServiceResult(result, "{业务名称}创建成功");
            }
            catch (Exception ex)
            {
                return HandleException<{EntityName}Dto>(ex, "创建{业务名称}", dto);
            }
        }

        /// <summary>
        /// 更新{业务名称}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<{EntityName}Dto>>> Update(Guid id, [FromBody] {EntityName}UpdateDto dto)
        {
            try
            {
                var idValidation = ValidateGuid<{EntityName}Dto>(id, "{业务名称}ID");
                if (idValidation != null) return idValidation;

                var modelValidation = ValidateModel<{EntityName}Dto>();
                if (modelValidation != null) return modelValidation;

                var result = await _{entityName}Service.UpdateAsync(id, dto);
                
                if (result.IsSuccess)
                {
                    LogOperation("更新{业务名称}", dto, id);
                }
                
                return HandleServiceResult(result, "{业务名称}更新成功");
            }
            catch (Exception ex)
            {
                return HandleException<{EntityName}Dto>(ex, "更新{业务名称}", new { id, dto });
            }
        }

        /// <summary>
        /// 删除{业务名称}（软删除）
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            try
            {
                var validation = ValidateGuid(id, "{业务名称}ID");
                if (validation != null) return validation;

                var result = await _{entityName}Service.DeleteAsync(id);
                
                if (result.IsSuccess)
                {
                    LogOperation("删除{业务名称}", null, id);
                }
                
                return HandleBoolServiceResult(result, "删除成功", "删除失败");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除{业务名称}", id);
            }
        }

        #endregion

        #region 业务功能操作

        /// <summary>
        /// 搜索{业务名称} - 关键词搜索
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<PagedApiResponse<{EntityName}Dto>>> Search(
            [FromQuery] string keyword, 
            [FromQuery] int pageIndex = 1, 
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return ValidationFailPaged<{EntityName}Dto>("搜索关键词不能为空");

                if (pageSize <= 0 || pageSize > 100)
                    return ValidationFailPaged<{EntityName}Dto>("页面大小必须在1-100之间");

                var query = new PagedQueryBaseDto
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    Keyword = keyword
                };

                var result = await _{entityName}Service.SearchAsync(query);
                return HandlePagedServiceResult(result, "搜索成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<{EntityName}Dto>(ex, "搜索{业务名称}", new { keyword, pageIndex, pageSize });
            }
        }

        /// <summary>
        /// 批量操作{业务名称} - 批量启用/禁用
        /// </summary>
        [HttpPatch("batch-status")]
        public async Task<ActionResult<ApiResponse>> BatchUpdateStatus([FromBody] Batch{EntityName}StatusDto dto)
        {
            try
            {
                var validation = ValidateModel();
                if (validation != null) return validation;

                if (dto.Ids == null || !dto.Ids.Any())
                    return ValidationFail("请选择要操作的{业务名称}");

                var result = await _{entityName}Service.BatchUpdateStatusAsync(dto.Ids, dto.Status, dto.Reason);
                
                if (result.IsSuccess)
                {
                    LogOperation("批量更新{业务名称}状态", dto, null);
                }
                
                return HandleBoolServiceResult(result, $"批量操作成功，共处理{dto.Ids.Count}条记录", "批量操作失败");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量更新{业务名称}状态", dto);
            }
        }

        /// <summary>
        /// 获取{业务名称}统计信息
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponse<{EntityName}StatisticsDto>>> GetStatistics()
        {
            try
            {
                var result = await _{entityName}Service.GetStatisticsAsync();
                return HandleServiceResult(result, "统计信息获取成功");
            }
            catch (Exception ex)
            {
                return HandleException<{EntityName}StatisticsDto>(ex, "获取{业务名称}统计信息", null);
            }
        }

        #endregion

        #region 辅助功能

        /// <summary>
        /// 获取{业务名称}选项列表 - 下拉框数据源
        /// </summary>
        [HttpGet("options")]
        public async Task<ActionResult<ApiResponse<List<{EntityName}OptionDto>>>> GetOptions()
        {
            try
            {
                var result = await _{entityName}Service.GetOptionsAsync();
                return HandleServiceResult(result, "选项列表获取成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<{EntityName}OptionDto>>(ex, "获取{业务名称}选项列表", null);
            }
        }

        /// <summary>
        /// 验证{业务名称}数据 - 表单验证
        /// </summary>
        [HttpPost("validate")]
        public async Task<ActionResult<ApiResponse<ValidationResultDto>>> Validate([FromBody] {EntityName}ValidateDto dto)
        {
            try
            {
                var validation = ValidateModel<ValidationResultDto>();
                if (validation != null) return validation;

                var result = await _{entityName}Service.ValidateAsync(dto);
                return HandleServiceResult(result, "验证完成");
            }
            catch (Exception ex)
            {
                return HandleException<ValidationResultDto>(ex, "验证{业务名称}数据", dto);
            }
        }

        #endregion
    }

    #region 请求DTO定义

    /// <summary>
    /// 批量状态更新请求
    /// </summary>
    public class Batch{EntityName}StatusDto
    {
        /// <summary>
        /// {业务名称}ID列表
        /// </summary>
        public List<Guid> Ids { get; set; } = new();

        /// <summary>
        /// 目标状态
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 操作原因
        /// </summary>
        public string? Reason { get; set; }
    }

    #endregion
}
```

---

## 🖥️ 系统管理控制器模板

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LYBT.Infrastructure.Web;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// {系统功能}管理控制器 - UltraThink系统架构
    /// 提供{系统功能}相关的系统管理接口
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = "Admin")] // 系统管理通常需要管理员权限
    public class {SystemName}Controller : BaseSystemController
    {
        private readonly I{SystemName}Service _{systemName}Service;

        /// <summary>
        /// 构造方法 - 注入{系统功能}服务
        /// </summary>
        public {SystemName}Controller(
            I{SystemName}Service {systemName}Service,
            ILogger<{SystemName}Controller> logger)
            : base(logger)
        {
            _{systemName}Service = {systemName}Service;
        }

        #region 状态查询

        /// <summary>
        /// 获取{系统功能}状态
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var status = await _{systemName}Service.GetStatusAsync();
                return SystemOk(status, "{系统功能}状态正常");
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "获取{系统功能}状态");
            }
        }

        /// <summary>
        /// 获取{系统功能}详细信息
        /// </summary>
        [HttpGet("details")]
        public async Task<IActionResult> GetDetails()
        {
            try
            {
                var details = await _{systemName}Service.GetDetailsAsync();
                return SystemOk(details, "详细信息获取成功");
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "获取{系统功能}详细信息");
            }
        }

        #endregion

        #region 系统操作

        /// <summary>
        /// 重启{系统功能}服务
        /// </summary>
        [HttpPost("restart")]
        public async Task<IActionResult> Restart()
        {
            try
            {
                if (!IsSystemAdmin())
                    return SystemError("权限不足，需要系统管理员权限", 403);

                var result = await _{systemName}Service.RestartAsync();
                
                if (result)
                {
                    LogOperation("重启{系统功能}服务", null, null);
                    return SystemOk("服务重启成功");
                }
                else
                {
                    return SystemError("服务重启失败");
                }
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "重启{系统功能}服务");
            }
        }

        /// <summary>
        /// 清理{系统功能}缓存
        /// </summary>
        [HttpPost("clear-cache")]
        public async Task<IActionResult> ClearCache()
        {
            try
            {
                var result = await _{systemName}Service.ClearCacheAsync();
                
                if (result)
                {
                    LogOperation("清理{系统功能}缓存", null, null);
                    return SystemOk("缓存清理成功");
                }
                else
                {
                    return SystemError("缓存清理失败");
                }
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "清理{系统功能}缓存");
            }
        }

        #endregion

        #region 配置管理

        /// <summary>
        /// 获取{系统功能}配置
        /// </summary>
        [HttpGet("configuration")]
        public async Task<IActionResult> GetConfiguration()
        {
            try
            {
                var config = await _{systemName}Service.GetConfigurationAsync();
                return SystemOk(config, "配置获取成功");
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "获取{系统功能}配置");
            }
        }

        /// <summary>
        /// 更新{系统功能}配置
        /// </summary>
        [HttpPut("configuration")]
        public async Task<IActionResult> UpdateConfiguration([FromBody] {SystemName}ConfigurationDto config)
        {
            try
            {
                var validation = ValidateSystemParameters(
                    (config != null, "配置信息不能为空")
                );
                if (validation != null) return validation;

                var result = await _{systemName}Service.UpdateConfigurationAsync(config!);
                
                if (result)
                {
                    LogOperation("更新{系统功能}配置", config, null);
                    return SystemOk("配置更新成功");
                }
                else
                {
                    return SystemError("配置更新失败");
                }
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "更新{系统功能}配置", config);
            }
        }

        #endregion

        #region 健康检查

        /// <summary>
        /// {系统功能}健康检查
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous] // 健康检查通常允许匿名访问
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var healthStatus = await _{systemName}Service.GetHealthAsync();
                
                var response = new
                {
                    status = healthStatus.IsHealthy ? "Healthy" : "Unhealthy",
                    {systemName} = new
                    {
                        status = healthStatus.Status,
                        details = healthStatus.Details,
                        checkedAt = healthStatus.CheckedAt
                    },
                    systemInfo = GetSystemInfo()
                };

                return healthStatus.IsHealthy ? 
                    SystemOk(response, "{系统功能}健康") : 
                    SystemError("{系统功能}异常", 503);
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "{系统功能}健康检查");
            }
        }

        #endregion
    }

    #region DTO定义

    /// <summary>
    /// {系统功能}配置DTO
    /// </summary>
    public class {SystemName}ConfigurationDto
    {
        /// <summary>
        /// 配置项1
        /// </summary>
        public string Setting1 { get; set; } = string.Empty;

        /// <summary>
        /// 配置项2
        /// </summary>
        public int Setting2 { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }
    }

    #endregion
}
```

---

## 📤 导入导出控制器模板

```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.{ModuleName};
using LYBT.Module.{ModuleName}.Interfaces;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// {业务名称}导入导出控制器 - UltraThink专用架构
    /// 专门负责{业务名称}的导入导出功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/{moduleName}")]
    [Authorize]
    public class {EntityName}ImportExportController : BaseApiController
    {
        private readonly I{EntityName}Service _{entityName}Service;
        private readonly ICacheService _cacheService;

        /// <summary>
        /// 构造方法 - 注入服务
        /// </summary>
        public {EntityName}ImportExportController(
            I{EntityName}Service {entityName}Service, 
            ICacheService cacheService,
            ILogger<{EntityName}ImportExportController> logger)
            : base(logger)
        {
            _{entityName}Service = {entityName}Service;
            _cacheService = cacheService;
        }

        #region 导入功能

        /// <summary>
        /// 批量导入{业务名称}
        /// </summary>
        [HttpPost("import")]
        public async Task<ActionResult<ApiResponse>> Import([FromBody] List<{EntityName}ImportDto> dtos)
        {
            try
            {
                if (dtos == null || dtos.Count == 0)
                {
                    return ValidationFail("导入数据不能为空");
                }

                // 数据验证
                var invalidItems = ValidateImportData(dtos);
                if (invalidItems.Any())
                {
                    return ValidationFail($"存在 {invalidItems.Count} 条无效数据", invalidItems);
                }

                var result = await _{entityName}Service.ImportAsync(dtos);

                if (result.IsSuccess)
                {
                    // 清除相关缓存
                    await _cacheService.RemoveByPatternAsync("{entityName}");
                    
                    LogOperation("批量导入{业务名称}", new { Count = result.Data, TotalSubmitted = dtos.Count }, null);
                }

                return HandleServiceResult(result, $"导入完成，成功处理{result.Data}条记录");
            }
            catch (ArgumentException ex)
            {
                return ValidationFail(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BusinessFail(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量导入{业务名称}", new { Count = dtos?.Count });
            }
        }

        /// <summary>
        /// 验证导入数据有效性
        /// </summary>
        [HttpPost("validate-import")]
        public async Task<ActionResult<ApiResponse>> ValidateImport([FromBody] List<{EntityName}ImportDto> dtos)
        {
            try
            {
                if (dtos == null || dtos.Count == 0)
                {
                    return ValidationFail("验证数据不能为空");
                }

                var validationResults = await ValidateImportDataAsync(dtos);
                
                var response = new
                {
                    totalCount = dtos.Count,
                    validCount = validationResults.Count(v => v.IsValid),
                    invalidCount = validationResults.Count(v => !v.IsValid),
                    results = validationResults
                };

                return Success(response, "数据验证完成");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "验证导入数据", new { Count = dtos?.Count });
            }
        }

        #endregion

        #region 导出功能

        /// <summary>
        /// 导出{业务名称}数据
        /// </summary>
        [HttpGet("export")]
        public async Task<ActionResult<ApiResponse<List<{EntityName}ExportDto>>>> Export([FromQuery] {EntityName}ExportQueryDto query)
        {
            try
            {
                var result = await _{entityName}Service.ExportAsync(query);
                
                if (result.IsSuccess)
                {
                    LogOperation("导出{业务名称}数据", new { Count = result.Data?.Count, Query = query }, null);
                }

                return HandleServiceResult(result, "导出成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<{EntityName}ExportDto>>(ex, "导出{业务名称}数据", query);
            }
        }

        /// <summary>
        /// 导出{业务名称}模板
        /// </summary>
        [HttpGet("export-template")]
        public async Task<ActionResult<ApiResponse<object>>> ExportTemplate()
        {
            try
            {
                var template = new
                {
                    // 定义模板字段
                    field1 = "示例值1",
                    field2 = "示例值2",
                    field3 = 0,
                    remark = "备注信息"
                };

                var templateData = new List<object> { template };
                
                LogOperation("导出{业务名称}导入模板", null, null);
                
                var response = new 
                { 
                    message = "{业务名称}导入模板",
                    template = templateData,
                    instructions = new
                    {
                        tips = new[]
                        {
                            "请按照模板格式填写数据",
                            "必填字段不能为空",
                            "数据格式请严格按照示例"
                        }
                    }
                };

                return Success(response, "模板导出成功");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "导出{业务名称}导入模板", null);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 验证导入数据
        /// </summary>
        private List<ImportValidationError> ValidateImportData(List<{EntityName}ImportDto> dtos)
        {
            var errors = new List<ImportValidationError>();
            
            for (int i = 0; i < dtos.Count; i++)
            {
                var dto = dtos[i];
                var itemErrors = new List<string>();

                // 基本字段验证
                if (string.IsNullOrWhiteSpace(dto.Name))
                    itemErrors.Add("名称不能为空");

                // 添加更多验证规则...

                if (itemErrors.Any())
                {
                    errors.Add(new ImportValidationError
                    {
                        RowIndex = i + 1,
                        Item = dto,
                        Errors = itemErrors
                    });
                }
            }

            return errors;
        }

        /// <summary>
        /// 异步验证导入数据（包含数据库检查）
        /// </summary>
        private async Task<List<ImportValidationResult>> ValidateImportDataAsync(List<{EntityName}ImportDto> dtos)
        {
            var results = new List<ImportValidationResult>();

            for (int i = 0; i < dtos.Count; i++)
            {
                var dto = dtos[i];
                var result = new ImportValidationResult
                {
                    RowIndex = i + 1,
                    Name = dto.Name ?? "未知",
                    IsValid = true,
                    Errors = new List<string>()
                };

                // 基本验证
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    result.Errors.Add("名称不能为空");
                    result.IsValid = false;
                }

                // 数据库验证（检查重复等）
                var existsResult = await _{entityName}Service.CheckExistsAsync(dto.Name);
                if (existsResult.IsSuccess && existsResult.Data)
                {
                    result.Errors.Add("名称已存在");
                    result.IsValid = false;
                }

                results.Add(result);
            }

            return results;
        }

        #endregion
    }

    #region 内部类定义

    /// <summary>
    /// 导入验证错误
    /// </summary>
    public class ImportValidationError
    {
        public int RowIndex { get; set; }
        public object Item { get; set; } = null!;
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// 导入验证结果
    /// </summary>
    public class ImportValidationResult
    {
        public int RowIndex { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    #endregion
}
```

---

## 🔍 查询专用控制器模板

```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.{ModuleName};
using LYBT.Module.{ModuleName}.Interfaces;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// {业务名称}查询控制器 - UltraThink查询专用架构
    /// 专门负责{业务名称}的各类查询功能，优化查询性能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/{moduleName}/query")]
    [Authorize]
    public class {EntityName}QueryController : BaseApiController
    {
        private readonly I{EntityName}Service _{entityName}Service;
        private readonly string _cacheKeyPrefix = "{entityName}_query";

        /// <summary>
        /// 构造方法 - 注入查询服务
        /// </summary>
        public {EntityName}QueryController(
            I{EntityName}Service {entityName}Service, 
            ILogger<{EntityName}QueryController> logger,
            IMemoryCache cache)
            : base(logger, cache)
        {
            _{entityName}Service = {entityName}Service;
        }

        #region 基础查询

        /// <summary>
        /// 快速搜索{业务名称} - 支持模糊匹配
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<List<{EntityName}SearchResultDto>>>> QuickSearch(
            [FromQuery] string keyword,
            [FromQuery] int limit = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return ValidationFail<List<{EntityName}SearchResultDto>>("搜索关键词不能为空");

                if (limit <= 0 || limit > 50)
                    return ValidationFail<List<{EntityName}SearchResultDto>>("查询数量必须在1-50之间");

                // 尝试从缓存获取
                var cacheKey = $"{_cacheKeyPrefix}_search_{keyword}_{limit}";
                if (_cache?.TryGetValue(cacheKey, out var cached) == true && cached is List<{EntityName}SearchResultDto> cachedResult)
                {
                    return Success(cachedResult, "查询成功（缓存）");
                }

                var result = await _{entityName}Service.QuickSearchAsync(keyword, limit);
                
                // 缓存结果
                if (result.IsSuccess && result.Data != null)
                {
                    _cache?.Set(cacheKey, result.Data, TimeSpan.FromMinutes(5));
                }

                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<{EntityName}SearchResultDto>>(ex, "快速搜索{业务名称}", new { keyword, limit });
            }
        }

        /// <summary>
        /// 高级查询{业务名称} - 支持多条件筛选
        /// </summary>
        [HttpPost("advanced")]
        public async Task<ActionResult<PagedApiResponse<{EntityName}Dto>>> AdvancedQuery([FromBody] {EntityName}AdvancedQueryDto query)
        {
            try
            {
                var validation = ValidateModel<{EntityName}Dto>();
                if (validation != null) return validation;

                var result = await _{entityName}Service.AdvancedQueryAsync(query);
                return HandlePagedServiceResult(result, "高级查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<{EntityName}Dto>(ex, "高级查询{业务名称}", query);
            }
        }

        #endregion

        #region 统计查询

        /// <summary>
        /// 获取{业务名称}统计概览
        /// </summary>
        [HttpGet("overview")]
        public async Task<ActionResult<ApiResponse<{EntityName}OverviewDto>>> GetOverview()
        {
            try
            {
                var cacheKey = $"{_cacheKeyPrefix}_overview";
                if (_cache?.TryGetValue(cacheKey, out var cached) == true && cached is {EntityName}OverviewDto cachedOverview)
                {
                    return Success(cachedOverview, "概览获取成功（缓存）");
                }

                var result = await _{entityName}Service.GetOverviewAsync();
                
                if (result.IsSuccess && result.Data != null)
                {
                    _cache?.Set(cacheKey, result.Data, TimeSpan.FromMinutes(10));
                }

                return HandleServiceResult(result, "概览获取成功");
            }
            catch (Exception ex)
            {
                return HandleException<{EntityName}OverviewDto>(ex, "获取{业务名称}概览", null);
            }
        }

        /// <summary>
        /// 获取{业务名称}趋势数据
        /// </summary>
        [HttpGet("trends")]
        public async Task<ActionResult<ApiResponse<{EntityName}TrendDto>>> GetTrends(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string period = "day")
        {
            try
            {
                // 默认查询最近30天
                startDate ??= DateTime.Now.AddDays(-30);
                endDate ??= DateTime.Now;

                if (startDate > endDate)
                    return ValidationFail<{EntityName}TrendDto>("开始日期不能大于结束日期");

                var query = new {EntityName}TrendQueryDto
                {
                    StartDate = startDate.Value,
                    EndDate = endDate.Value,
                    Period = period
                };

                var result = await _{entityName}Service.GetTrendsAsync(query);
                return HandleServiceResult(result, "趋势数据获取成功");
            }
            catch (Exception ex)
            {
                return HandleException<{EntityName}TrendDto>(ex, "获取{业务名称}趋势", new { startDate, endDate, period });
            }
        }

        #endregion

        #region 关联查询

        /// <summary>
        /// 获取{业务名称}相关联的数据
        /// </summary>
        [HttpGet("{id}/related")]
        public async Task<ActionResult<ApiResponse<{EntityName}RelatedDataDto>>> GetRelatedData(Guid id)
        {
            try
            {
                var validation = ValidateGuid<{EntityName}RelatedDataDto>(id, "{业务名称}ID");
                if (validation != null) return validation;

                var result = await _{entityName}Service.GetRelatedDataAsync(id);
                return HandleServiceResult(result, "关联数据获取成功");
            }
            catch (Exception ex)
            {
                return HandleException<{EntityName}RelatedDataDto>(ex, "获取{业务名称}关联数据", id);
            }
        }

        /// <summary>
        /// 获取{业务名称}推荐列表
        /// </summary>
        [HttpGet("recommendations")]
        public async Task<ActionResult<ApiResponse<List<{EntityName}RecommendationDto>>>> GetRecommendations(
            [FromQuery] Guid? referenceId = null,
            [FromQuery] int count = 5)
        {
            try
            {
                if (count <= 0 || count > 20)
                    return ValidationFail<List<{EntityName}RecommendationDto>>("推荐数量必须在1-20之间");

                var result = await _{entityName}Service.GetRecommendationsAsync(referenceId, count);
                return HandleServiceResult(result, "推荐列表获取成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<{EntityName}RecommendationDto>>(ex, "获取{业务名称}推荐", new { referenceId, count });
            }
        }

        #endregion

        #region 缓存管理

        /// <summary>
        /// 清理查询缓存
        /// </summary>
        [HttpDelete("cache")]
        [Authorize(Roles = "Admin")]
        public IActionResult ClearQueryCache()
        {
            try
            {
                ClearCacheByPattern(_cacheKeyPrefix);
                LogOperation("清理{业务名称}查询缓存", null, null);
                return Ok(new { message = "查询缓存已清理" });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "清理{业务名称}查询缓存", null);
            }
        }

        #endregion
    }
}
```

---

## 🛠️ 使用说明

### 1. 模板变量替换

在使用模板时，需要替换以下变量：

| 变量 | 说明 | 示例 |
|------|------|------|
| `{EntityName}` | 实体名称（PascalCase） | `User`, `Patient`, `Herb` |
| `{entityName}` | 实体名称（camelCase） | `user`, `patient`, `herb` |
| `{ModuleName}` | 模块名称 | `Users`, `Patients`, `Herbs` |
| `{moduleName}` | 模块名称（小写） | `users`, `patients`, `herbs` |
| `{业务名称}` | 中文业务名称 | `用户`, `患者`, `药材` |
| `{SystemName}` | 系统名称（PascalCase） | `Cache`, `Health`, `Security` |
| `{systemName}` | 系统名称（camelCase） | `cache`, `health`, `security` |
| `{系统功能}` | 中文系统功能名 | `缓存`, `健康检查`, `安全` |

### 2. 快速创建脚本

```bash
# 创建业务控制器脚本示例
#!/bin/bash
ENTITY_NAME=$1
MODULE_NAME=$2
BUSINESS_NAME=$3

# 复制模板并替换变量
cp controller-template.cs ${ENTITY_NAME}Controller.cs
sed -i "s/{EntityName}/$ENTITY_NAME/g" ${ENTITY_NAME}Controller.cs
sed -i "s/{ModuleName}/$MODULE_NAME/g" ${ENTITY_NAME}Controller.cs
sed -i "s/{业务名称}/$BUSINESS_NAME/g" ${ENTITY_NAME}Controller.cs
```

### 3. Visual Studio 代码片段

在Visual Studio中创建代码片段：

```xml
<?xml version="1.0" encoding="utf-8"?>
<CodeSnippets>
  <CodeSnippet Format="1.0.0">
    <Header>
      <Title>UltraThink Business Controller</Title>
      <Shortcut>utcontroller</Shortcut>
    </Header>
    <Snippet>
      <Declarations>
        <Literal>
          <ID>EntityName</ID>
          <Default>Entity</Default>
        </Literal>
        <Literal>
          <ID>entityName</ID>
          <Default>entity</Default>
        </Literal>
      </Declarations>
      <Code Language="csharp">
        <![CDATA[/* 在此处插入业务控制器模板 */]]>
      </Code>
    </Snippet>
  </CodeSnippet>
</CodeSnippets>
```

---

## ✅ 检查清单

使用模板创建控制器后，请检查：

### 基础结构
- [ ] 继承正确的基类（BaseApiController/BaseSystemController）
- [ ] 添加正确的属性配置（[ApiController], [Route], [Authorize]等）
- [ ] 构造函数参数和调用基类构造函数
- [ ] using语句完整且正确

### 方法实现
- [ ] 所有公共方法都有异常处理
- [ ] 使用适当的响应方法（Success, HandleServiceResult等）
- [ ] 参数验证使用基类方法
- [ ] 重要操作记录日志

### 响应格式
- [ ] 业务API使用ApiResponse<T>格式
- [ ] 系统API使用简化响应格式
- [ ] 错误处理返回正确的状态码和消息

### 文档和注释
- [ ] 所有public方法都有XML注释
- [ ] 类级别注释说明控制器职责
- [ ] DTO类有适当的属性注释

---

## 📚 相关文档

- [控制器设计模式](../architecture/ultrathink-controller-design-patterns-20250817.md)
- [API响应标准](../architecture/ultrathink-api-response-standards-20250817.md)
- [最佳实践指南](../guides/controller-best-practices-20250817.md)
- [开发规范总览](../development/DEVELOPMENT_STANDARDS.md)

---

**使用提示**: 这些模板是标准化的起点，请根据具体业务需求进行适当调整，但要保持核心架构模式不变。