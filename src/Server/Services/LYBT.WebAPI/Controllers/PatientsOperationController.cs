using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 患者操作API - 处理导入、导出、批量操作等
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/patients/operation")]
    [Authorize]
    public class PatientsOperationController : BaseApiController
    {
        private readonly IPatientBusinessService _businessService;
        private readonly IPatientQueryService _queryService;

        public PatientsOperationController(
            IPatientBusinessService businessService,
            IPatientQueryService queryService,
            IMemoryCache cache,
            ILogger<PatientsOperationController> logger)
            : base(logger, cache)
        {
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        }

        /// <summary>
        /// 批量导入患者
        /// </summary>
        [HttpPost("import")]
        public async Task<ActionResult<ApiResponse<List<PatientDto>>>> ImportPatients([FromBody] List<PatientImportDto> patients)
        {
            try
            {
                if (patients == null || patients.Count == 0)
                {
                    return ValidationFail<List<PatientDto>>("导入数据不能为空");
                }

                if (patients.Count > 1000)
                {
                    return ValidationFail<List<PatientDto>>("单次导入数量不能超过1000条");
                }

                var result = await _businessService.ImportPatientsAsync(patients);
                if (!result.IsSuccess)
                {
                    return BusinessFail<List<PatientDto>>(result.ErrorMessage ?? "导入失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation($"批量导入患者成功，共 {result.Data?.Count ?? 0} 条", result.Data);
                return Success(result.Data ?? new List<PatientDto>(), $"成功导入 {result.Data?.Count ?? 0} 条患者记录");
            }
            catch (Exception ex)
            {
                return HandleException<List<PatientDto>>(ex, "批量导入患者", new { count = patients?.Count });
            }
        }

        /// <summary>
        /// 导出患者数据
        /// </summary>
        [HttpPost("export")]
        public async Task<ActionResult<ApiResponse<List<PatientDto>>>> ExportPatients([FromBody] PatientExportDto exportDto)
        {
            try
            {
                var validationResult = ValidateModel<List<PatientDto>>();
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _businessService.ExportPatientsAsync(exportDto);
                if (!result.IsSuccess)
                {
                    return BusinessFail<List<PatientDto>>(result.ErrorMessage ?? "导出失败", ApiErrorCodes.DATAEXPORTFAILED);
                }

                LogOperation($"导出患者数据成功，共 {result.Data?.Count ?? 0} 条", null);
                return Success(result.Data ?? new List<PatientDto>(), "导出成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<PatientDto>>(ex, "导出患者数据", exportDto);
            }
        }

        /// <summary>
        /// 批量删除患者
        /// </summary>
        [HttpPost("delete")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteBatch([FromBody] List<Guid> patientIds)
        {
            try
            {
                if (patientIds == null || patientIds.Count == 0)
                {
                    return ValidationFail<bool>("请选择要删除的患者");
                }

                if (patientIds.Count > 100)
                {
                    return ValidationFail<bool>("单次删除数量不能超过100条");
                }

                var result = await _businessService.DeleteAsync(patientIds);
                if (!result.IsSuccess)
                {
                    return BusinessFail<bool>(result.ErrorMessage ?? "批量删除失败", ApiErrorCodes.DATADELETEFAILED);
                }

                LogOperation($"批量删除患者成功，共 {patientIds.Count} 条", patientIds);
                return Success(true, $"成功删除 {patientIds.Count} 条患者记录");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "批量删除患者", new { count = patientIds?.Count });
            }
        }

        /// <summary>
        /// 批量启用患者
        /// </summary>
        [HttpPost("enable")]
        public async Task<ActionResult<ApiResponse<bool>>> EnableBatch([FromBody] List<Guid> patientIds)
        {
            try
            {
                if (patientIds == null || patientIds.Count == 0)
                {
                    return ValidationFail<bool>("请选择要启用的患者");
                }

                var result = await _businessService.EnableAsync(patientIds);
                if (!result.IsSuccess)
                {
                    return BusinessFail<bool>(result.ErrorMessage ?? "批量启用失败", ApiErrorCodes.DATAUPDATEFAILED);
                }

                LogOperation($"批量启用患者成功，共 {patientIds.Count} 条", patientIds);
                return Success(true, $"成功启用 {patientIds.Count} 条患者记录");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "批量启用患者", new { count = patientIds?.Count });
            }
        }

        /// <summary>
        /// 批量禁用患者
        /// </summary>
        [HttpPost("disable")]
        public async Task<ActionResult<ApiResponse<bool>>> DisableBatch([FromBody] List<Guid> patientIds)
        {
            try
            {
                if (patientIds == null || patientIds.Count == 0)
                {
                    return ValidationFail<bool>("请选择要禁用的患者");
                }

                var result = await _businessService.DisableAsync(patientIds);
                if (!result.IsSuccess)
                {
                    return BusinessFail<bool>(result.ErrorMessage ?? "批量禁用失败", ApiErrorCodes.DATAUPDATEFAILED);
                }

                LogOperation($"批量禁用患者成功，共 {patientIds.Count} 条", patientIds);
                return Success(true, $"成功禁用 {patientIds.Count} 条患者记录");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "批量禁用患者", new { count = patientIds?.Count });
            }
        }

        /// <summary>
        /// 批量设置患者状态
        /// </summary>
        [HttpPost("status")]
        public async Task<ActionResult<ApiResponse<bool>>> SetStatusBatch([FromBody] BatchStatusUpdateDto dto)
        {
            try
            {
                if (dto == null || dto.Ids == null || dto.Ids.Count == 0)
                {
                    return ValidationFail<bool>("请选择要更新的患者");
                }

                var result = await _businessService.SetStatusAsync(dto.Ids, dto.Status);
                if (!result.IsSuccess)
                {
                    return BusinessFail<bool>(result.ErrorMessage ?? "批量更新状态失败", ApiErrorCodes.DATAUPDATEFAILED);
                }

                LogOperation($"批量更新患者状态成功，共 {dto.Ids.Count} 条，状态：{dto.Status}", dto);
                return Success(true, $"成功更新 {dto.Ids.Count} 条患者状态");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "批量更新患者状态", dto);
            }
        }

        /// <summary>
        /// 获取导入模板
        /// </summary>
        [HttpGet("template")]
        public async Task<ActionResult<ApiResponse<object>>> GetImportTemplate()
        {
            try
            {
                var result = await _businessService.GetImportTemplate();
                if (!result.IsSuccess)
                {
                    return BusinessFail<object>(result.ErrorMessage ?? "获取模板失败", ApiErrorCodes.DATAQUERYFAILED);
                }

                return Success(result.Data ?? new { }, "获取模板成功");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "获取导入模板", null);
            }
        }

        /// <summary>
        /// 检查重复患者
        /// </summary>
        [HttpPost("check-duplicate")]
        public async Task<ActionResult<ApiResponse<List<PatientDto>>>> CheckDuplicate([FromBody] PatientCreateDto createDto)
        {
            try
            {
                var validationResult = ValidateModel<List<PatientDto>>();
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _queryService.CheckDuplicatePatientsAsync(createDto);
                if (!result.IsSuccess)
                {
                    return BusinessFail<List<PatientDto>>(result.ErrorMessage ?? "检查失败", ApiErrorCodes.DATAQUERYFAILED);
                }

                var message = result.Data != null && result.Data.Count > 0
                    ? $"发现 {result.Data.Count} 条可能重复的患者记录"
                    : "未发现重复患者";

                return Success(result.Data ?? new List<PatientDto>(), message);
            }
            catch (Exception ex)
            {
                return HandleException<List<PatientDto>>(ex, "检查重复患者", createDto);
            }
        }

        /// <summary>
        /// 高级搜索患者
        /// </summary>
        [HttpPost("advanced-search")]
        public async Task<ActionResult<ApiResponse<PagedResult<PatientDto>>>> AdvancedSearch([FromBody] PatientSearchDto searchDto)
        {
            try
            {
                var validationResult = ValidateModel<PagedResult<PatientDto>>();
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _queryService.AdvancedSearchAsync(searchDto);
                return HandlePagedServiceResult(result, "搜索成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<PatientDto>(ex, "高级搜索患者", searchDto);
            }
        }
    }

    /// <summary>
    /// 批量状态更新DTO
    /// </summary>
    public class BatchStatusUpdateDto
    {
        /// <summary>
        /// 要更新的ID列表
        /// </summary>
        public List<Guid> Ids { get; set; } = new();

        /// <summary>
        /// 目标状态
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}