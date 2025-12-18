using Asp.Versioning;
using AutoMapper;
using LYBT.Infrastructure.Web;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 患者管理 API - 基础CRUD功能
    /// </summary>
    /// optimize-api-permissions: 患者管理需Doctor或Admin角色
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "DoctorOrAdmin")]
    public class PatientsController : BaseApiController
    {
        private readonly IPatientService _service;
        private readonly IMapper _mapper;

        public PatientsController(IPatientService service, IMapper mapper, ILogger<PatientsController> logger)
            : base(logger)
        {
            _service = service;
            _mapper = mapper;
        }

        /// <summary>
        /// 获取患者列表 - 支持分页和查询
        /// </summary>
        [HttpGet]
        [OutputCache(PolicyName = "PatientsCache")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientDto>>), 200)]
        public async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFail("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var entityResult = await _service.GetPagedEntityAsync(page, pageSize, keyword);
                if (!entityResult.IsSuccess || entityResult.Data == null)
                {
                    return BusinessFail(entityResult.ErrorMessage ?? "查询失败");
                }

                var entityPagedResult = entityResult.Data;
                var patientDtos = _mapper.Map<List<PatientDto>>(entityPagedResult.Items);

                foreach (var item in patientDtos)
                {
                    var entity = entityPagedResult.Items.FirstOrDefault(e => e.Id == item.Id);
                    if (entity != null)
                    {
                        item.Age = entity.Age;
                    }
                }

                var dtoPagedResult = new PagedResult<PatientDto>
                {
                    Items = patientDtos,
                    TotalCount = entityPagedResult.TotalCount,
                    CurrentPage = entityPagedResult.CurrentPage,
                    PageSize = entityPagedResult.PageSize
                };

                return SuccessPaged(dtoPagedResult, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取患者列表", new { page, pageSize, keyword });
            }
        }

        /// <summary>
        /// 获取患者列表（分页，返回PatientListDto，用于列表视图）
        /// OpenSpec: optimize-entity-data-flow - 增量API方法
        /// </summary>
        [HttpGet("list")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientListDto>>), 200)]
        public async Task<IActionResult> GetPatientsList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFail("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var result = await _service.GetPagedListAsync(page, pageSize, keyword);
                return HandlePagedResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取患者列表", new { page, pageSize, keyword });
            }
        }

        /// <summary>
        /// 获取患者详情
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                if (ValidateGuid(id, "患者ID") is { } error) return error;

                var entityResult = await _service.GetByIdEntityAsync(id);
                if (!entityResult.IsSuccess || entityResult.Data == null)
                {
                    return NotFound(entityResult.ErrorMessage ?? "患者不存在");
                }

                var patientEntity = entityResult.Data;
                var patientDto = _mapper.Map<PatientDto>(patientEntity);
                patientDto.Age = patientEntity.Age;

                return Success(patientDto, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取患者详情", id);
            }
        }

        /// <summary>
        /// 新增患者
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
        public async Task<IActionResult> Add([FromBody] PatientInputDto dto)
        {
            try
            {
                var entityResult = await _service.CreateEntityAsync(dto);
                if (!entityResult.IsSuccess || entityResult.Data == null)
                {
                    return ValidationFail(entityResult.ErrorMessage ?? "新增患者失败");
                }

                var patientEntity = entityResult.Data;
                var patientDto = _mapper.Map<PatientDto>(patientEntity);
                patientDto.Age = patientEntity.Age;

                LogOperation("新增患者成功", patientDto, patientEntity.Id);
                return Success(patientDto, "患者创建成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "新增患者", dto);
            }
        }

        /// <summary>
        /// 更新患者信息
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
        public async Task<IActionResult> Update(Guid id, [FromBody] PatientInputDto dto)
        {
            try
            {
                // 使用统一的所有权检查方法（DTO版本）
                var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(
                    id, _service.GetByIdAsync, "患者");
                if (ownershipError != null) return ownershipError;

                var entityResult = await _service.UpdateEntityAsync(id, dto);
                if (!entityResult.IsSuccess || entityResult.Data == null)
                {
                    if (entityResult.ErrorMessage?.Contains("不存在") == true)
                    {
                        return NotFound(entityResult.ErrorMessage);
                    }
                    return ValidationFail(entityResult.ErrorMessage ?? "更新患者失败");
                }

                var patientEntity = entityResult.Data;
                var patientDto = _mapper.Map<PatientDto>(patientEntity);
                patientDto.Age = patientEntity.Age;

                LogOperation("更新患者成功", patientDto, id);
                return Success(patientDto, "患者更新成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "更新患者", new { id, dto });
            }
        }

        /// <summary>
        /// 删除患者（软删除）
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                // 使用统一的所有权检查方法（DTO版本）
                var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(
                    id, _service.GetByIdAsync, "患者");
                if (ownershipError != null) return ownershipError;

                var result = await _service.DeleteAsync(id);
                if (!result.IsSuccess)
                {
                    return NotFound("患者不存在");
                }

                LogOperation("删除患者成功", null, id);
                return Success(true, "删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除患者", id);
            }
        }

        /// <summary>
        /// 批量导入患者数据
        /// </summary>
        [HttpPost("import")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [ProducesResponseType(typeof(ApiResponse<BatchImportResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<BatchImportResultDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Import(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return ValidationFail("文件不能为空");
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".xlsx")
                {
                    return ValidationFail("仅支持.xlsx格式的Excel文件");
                }

                if (file.Length > 10 * 1024 * 1024)
                {
                    return ValidationFail("文件大小不能超过10MB");
                }

                using var stream = file.OpenReadStream();
                var result = await _service.BatchImportAsync(stream, file.FileName);

                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail(result.ErrorMessage ?? "导入失败");
                }

                LogOperation("批量导入患者",
                    new { FileName = file.FileName, SuccessCount = result.Data.SuccessCount, FailureCount = result.Data.FailureCount, SkippedCount = result.Data.SkippedCount },
                    null);

                return Success(result.Data);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量导入患者", new { FileName = file?.FileName });
            }
        }

        /// <summary>
        /// 下载患者导入模板
        /// </summary>
        [HttpGet("import-template")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<IActionResult> ExportTemplate()
        {
            try
            {
                var config = new ExportTemplateDto
                {
                    IncludeSampleData = true,
                    SampleRowCount = 3
                };
                var stream = await _service.ExportTemplateAsync(config);
                var fileName = $"患者导入模板_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成患者导入模板失败");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// 导出患者数据到Excel
        /// </summary>
        [HttpGet("export")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<IActionResult> ExportPatients([FromQuery] string? keyword = null)
        {
            try
            {
                var stream = await _service.ExportPatientsAsync(keyword);
                var fileName = string.IsNullOrWhiteSpace(keyword)
                    ? $"患者数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    : $"患者数据_{keyword}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                LogOperation("导出患者数据", new { Keyword = keyword }, null);

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出患者数据失败，关键词：{Keyword}", keyword);
                return StatusCode(500);
            }
        }

        // ========== OpenSpec: optimize-module-list-ui - 恢复端点 ==========

        /// <summary>
        /// 恢复已删除的患者
        /// 注：患者实体无Status字段，因此无ToggleStatus端点
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpPost("{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Restore(Guid id)
        {
            try
            {
                // 使用统一的所有权检查方法（DTO版本）
                var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(
                    id, _service.GetByIdAsync, "患者");
                if (ownershipError != null) return ownershipError;

                var result = await _service.RestoreAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail(result.ErrorMessage ?? "恢复失败");
                }

                LogOperation("恢复患者", null, id);
                return Success(result.Data, "患者已恢复");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "恢复患者", id);
            }
        }
    }
}
