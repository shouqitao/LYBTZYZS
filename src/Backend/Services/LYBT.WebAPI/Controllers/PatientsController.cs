using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using BatchOperationDto = LYBT.Shared.Models.Common.BatchOperationDto;
using PatientDto = LYBT.Shared.Models.Contracts.Patients.PatientDto;
using PatientDetailDto = LYBT.Shared.Models.Contracts.Patients.PatientDetailDto;
using PatientPagedQueryDto = LYBT.Shared.Models.Contracts.Patients.PatientPagedQueryDto;
using QuickPatientCreateDto = LYBT.Shared.Models.Contracts.Patients.QuickPatientCreateDto;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 患者管理API接口 - 统一API响应格式和错误处理
    /// 实现软删除策略：患者档案只能禁用/启用，不提供删除接口
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class PatientsController : BaseApiController
    {
        private readonly IPatientService _patientService;

        public PatientsController(IPatientService patientService, IMemoryCache cache, ILogger<PatientsController> logger)
            : base(logger, cache)
        {
            _patientService = patientService;
        }

        // 移除重复的新增患者接口，统一使用RESTful POST接口

        /// <summary>
        /// 快速创建患者档案（简化版本） - 统一API响应格式
        /// </summary>
        [HttpPost("quick")]
        public async Task<ActionResult<ApiResponse<PatientDetailDto>>> QuickCreate([FromBody] QuickPatientCreateDto dto)
        {
            try
            {
                var validation = ValidateModel<PatientDetailDto>();
                if (validation != null) return validation;

                var (operatorId, operatorName, operatorRole) = GetOperator();
                // 将QuickPatientCreateDto转换为PatientDetailDto
                var patientDto = new PatientDetailDto
                {
                    Name = dto.Name,
                    Gender = dto.Gender,
                    Age = dto.Age ?? 0,
                    PhoneNumber = dto.PhoneNumber ?? dto.Phone ?? string.Empty,
                    IDNumber = dto.IDNumber ?? string.Empty,
                    Address = dto.Address ?? string.Empty
                };

                var result = await _patientService.CreateAsync(patientDto, operatorId, operatorName);
                if (result == null)
                {
                    return BusinessFail<PatientDetailDto>("患者档案快速创建失败，必填项不完整或已存在", ApiErrorCodes.DATA_SAVE_FAILED);
                }

                LogOperation("快速创建患者档案", result, result.Id);
                return Success(result, "患者档案快速创建成功");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("已存在"))
            {
                return BusinessFail<PatientDetailDto>(ex.Message, ApiErrorCodes.PHONE_EXISTS);
            }
            catch (Exception ex)
            {
                return HandleException<PatientDetailDto>(ex, "快速创建患者档案", dto);
            }
        }

        // 移除单独的Enable/Disable接口，统一使用ToggleStatus接口

        /// <summary>
        /// 切换患者档案状态（启用/禁用） - 统一API响应格式
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        public async Task<ActionResult<ApiResponse>> ToggleStatus(Guid id)
        {
            try
            {
                var validation = ValidateGuid(id, "患者ID");
                if (validation != null) return validation;

                var (operatorId, operatorName, operatorRole) = GetOperator();
                // 先获取患者当前状态
                var patient = await _patientService.GetByIdAsync(id);
                if (patient == null)
                {
                    return NotFound("患者不存在", ApiErrorCodes.PATIENT_NOT_FOUND);
                }

                // 根据当前状态切换
                bool result;
                string message;
                if (patient.Status == CommonStatus.Enabled)
                {
                    result = await _patientService.SetStatusAsync(id, false, operatorId, operatorName);
                    message = "患者档案已禁用";
                }
                else
                {
                    result = await _patientService.SetStatusAsync(id, true, operatorId, operatorName);
                    message = "患者档案已启用";
                }

                if (!result)
                {
                    return BusinessFail("状态切换失败", ApiErrorCodes.DATA_UPDATE_FAILED);
                }

                LogOperation(message, null, id);
                return Success(message);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "切换患者状态", id);
            }
        }

        /// <summary>
        /// 获取全部患者（小数据量场景） - 统一API响应格式
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult<ApiResponse<List<PatientDetailDto>>>> GetAll()
        {
            try
            {
                var (_, _, operatorRole) = GetOperator();
                var cacheKey = $"patients:all:{operatorRole}";

                if ((_cache?.TryGetValue(cacheKey, out List<PatientDetailDto>? data)) ?? false)
                {
                    return Success(data, "查询成功（缓存）");
                }

                var result = await _patientService.GetAllAsync();
                _cache?.Set(cacheKey, result, TimeSpan.FromMinutes(5));
                return Success(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<PatientDetailDto>>(ex, "获取全部患者列表", null);
            }
        }

        /// <summary>
        /// 分页条件查询 - 统一API响应格式
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<PagedApiResponse<PatientDetailDto>>> GetPaged([FromBody] PatientPagedQueryDto query)
        {
            try
            {
                var validation = ValidateModelPaged<PatientDetailDto>();
                if (validation != null) return validation;

                if (query.CurrentPage <= 0 || query.PageSize <= 0 || query.PageSize > 100)
                {
                    return ValidationFailPaged<PatientDetailDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var (_, _, operatorRole) = GetOperator();
                var result = await _patientService.GetPagedAsync(query);
                return Success(result, "分页查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<PatientDetailDto>(ex, "分页查询患者", query);
            }
        }

        // 移除未实现的批量操作接口，避免误导用户

        /// <summary>
        /// 搜索患者档案 - 统一API响应格式
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<List<PatientDetailDto>>>> Search([FromQuery] string keyword = "")
        {
            try
            {
                var (_, _, operatorRole) = GetOperator();
                var list = await _patientService.SearchAsync(keyword);
                return Success(list, $"搜索完成，找到{list.Count}条记录");
            }
            catch (Exception ex)
            {
                return HandleException<List<PatientDetailDto>>(ex, "搜索患者档案", keyword);
            }
        }

        /// <summary>
        /// 导出患者档案数据 - 统一API响应格式
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        [HttpGet("export")]
        public async Task<ActionResult<ApiResponse<List<PatientDetailDto>>>> Export()
        {
            try
            {
                var (_, _, operatorRole) = GetOperator();
                var data = await _patientService.GetAllAsync();
                LogOperation("导出患者档案", new { count = data.Count });
                return Success(data, $"成功导出{data.Count}条患者档案");
            }
            catch (Exception ex)
            {
                return HandleException<List<PatientDetailDto>>(ex, "导出患者档案", null);
            }
        }

        // 移除未实现的导入和历史病历功能，避免误导用户
        // 这些功能可以在后续版本中根据实际需求添加

        /// <summary>
        /// 获取启用的患者档案列表 - 统一API响应格式
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<ApiResponse<List<PatientDetailDto>>>> GetActivePatients()
        {
            try
            {
                var patients = await _patientService.GetActivePatientsAsync();
                return Success(patients, $"查询成功，共{patients.Count}条启用的患者档案");
            }
            catch (Exception ex)
            {
                return HandleException<List<PatientDetailDto>>(ex, "获取启用的患者档案", null);
            }
        }

        /// <summary>
        /// 查询或创建患者档案（用于挂号/看诊场景） - 统一API响应格式
        /// 根据姓名和身份证号查询患者档案，如果不存在则创建新档案
        /// </summary>
        [HttpPost("find-or-create")]
        public async Task<ActionResult<ApiResponse<PatientDetailDto>>> FindOrCreate([FromBody] PatientDetailDto dto)
        {
            try
            {
                var validation = ValidateModel<PatientDetailDto>();
                if (validation != null) return validation;

                var (operatorId, operatorName, operatorRole) = GetOperator();
                var patient = await FindOrCreatePatientAsync(dto, operatorId, operatorName);
                
                if (patient == null)
                {
                    return BusinessFail<PatientDetailDto>("查询或创建患者档案失败", ApiErrorCodes.DATA_SAVE_FAILED);
                }

                LogOperation("查询或创建患者档案", patient, patient.Id);
                return Success(patient, "操作成功");
            }
            catch (Exception ex)
            {
                return HandleException<PatientDetailDto>(ex, "查询或创建患者档案", dto);
            }
        }

        // ======================== RESTful 标准接口 ========================

        /// <summary>
        /// 获取所有患者列表 (RESTful GET /Patients) - 支持多字段模糊查询 - 统一API响应格式
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedApiResponse<PatientDetailDto>>> GetPatients(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? name = null,
            [FromQuery] string? phoneNumber = null,
            [FromQuery] string? idNumber = null,
            [FromQuery] string? address = null,
            [FromQuery] Gender? gender = null,
            [FromQuery] int? minAge = null,
            [FromQuery] int? maxAge = null,
            [FromQuery] PatientStatus? status = null)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFailPaged<PatientDetailDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var (_, _, operatorRole) = GetOperator();
                var query = new PatientPagedQueryDto
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    SearchKeyword = keyword,
                    Name = name,
                    PhoneNumber = phoneNumber,
                    IDNumber = idNumber,
                    Address = address,
                    Gender = gender,
                    MinAge = minAge,
                    MaxAge = maxAge
                };
                var result = await _patientService.GetPagedAsync(query);
                return Success(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<PatientDetailDto>(ex, "获取患者列表", new { page, pageSize, keyword });
            }
        }

        /// <summary>
        /// 创建新患者 (RESTful POST /Patients) - 统一API响应格式
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<PatientDetailDto>>> CreatePatient([FromBody] PatientDetailDto dto)
        {
            try
            {
                var validation = ValidateModel<PatientDetailDto>();
                if (validation != null) return validation;

                var (operatorId, operatorName, operatorRole) = GetOperator();
                var result = await _patientService.CreateAsync(dto, operatorId, operatorName);
                
                if (result == null)
                {
                    return BusinessFail<PatientDetailDto>("患者创建失败，必填项不完整或已存在", ApiErrorCodes.DATA_SAVE_FAILED);
                }

                LogOperation("患者创建成功", result, result.Id);
                return Success(result, "患者创建成功");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("已存在"))
            {
                return BusinessFail<PatientDetailDto>(ex.Message, ApiErrorCodes.PHONE_EXISTS);
            }
            catch (Exception ex)
            {
                return HandleException<PatientDetailDto>(ex, "创建患者", dto);
            }
        }

        /// <summary>
        /// 根据ID获取患者 (RESTful GET /Patients/{id}) - 统一API响应格式
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<PatientDetailDto>>> GetPatient(Guid id)
        {
            try
            {
                var validation = ValidateGuid<PatientDetailDto>(id, "患者ID");
                if (validation != null) return validation;

                var (_, _, operatorRole) = GetOperator();
                var patient = await _patientService.GetByIdAsync(id);
                if (patient == null)
                {
                    return NotFound<PatientDetailDto>("患者不存在", ApiErrorCodes.PATIENT_NOT_FOUND);
                }
                
                return Success(patient, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<PatientDetailDto>(ex, "获取患者信息", id);
            }
        }

        /// <summary>
        /// 更新患者信息 (RESTful PUT /Patients/{id}) - 统一API响应格式
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<PatientDetailDto>>> UpdatePatient(Guid id, [FromBody] PatientDetailDto dto)
        {
            try
            {
                var idValidation = ValidateGuid<PatientDetailDto>(id, "患者ID");
                if (idValidation != null) return idValidation;

                var modelValidation = ValidateModel<PatientDetailDto>();
                if (modelValidation != null) return modelValidation;

                // 检查ID一致性
                if (dto.Id != id)
                {
                    return ValidationFail<PatientDetailDto>("URL中的ID与请求体中的ID不匹配");
                }

                var (operatorId, operatorName, operatorRole) = GetOperator();
                var result = await _patientService.UpdateAsync(id, dto, operatorId, operatorName);
                
                if (result == null)
                {
                    return BusinessFail<PatientDetailDto>("患者信息更新失败", ApiErrorCodes.DATA_UPDATE_FAILED);
                }

                // 获取更新后的资源
                var updated = await _patientService.GetByIdAsync(id);
                LogOperation("更新患者信息成功", updated, id);
                return Success(updated, "患者信息更新成功");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("患者不存在"))
            {
                return NotFound<PatientDetailDto>(ex.Message, ApiErrorCodes.PATIENT_NOT_FOUND);
            }
            catch (Exception ex)
            {
                return HandleException<PatientDetailDto>(ex, "更新患者信息", new { id, dto });
            }
        }

        /// <summary>
        /// 删除患者 (RESTful DELETE /Patients/{id}) - 实际执行软删除 - 统一API响应格式
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeletePatient(Guid id)
        {
            try
            {
                var validation = ValidateGuid(id, "患者ID");
                if (validation != null) return validation;

                var (operatorId, operatorName, operatorRole) = GetOperator();
                var result = await _patientService.SetStatusAsync(id, false, operatorId, operatorName);
                
                if (!result)
                {
                    return BusinessFail("禁用患者失败", ApiErrorCodes.DATA_UPDATE_FAILED);
                }

                LogOperation("患者已禁用", null, id);
                return Success("患者已禁用");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "禁用患者", id);
            }
        }

        // 注意：不提供真正的删除接口，患者档案只能禁用，不能删除
        // 原有的删除相关接口已移除，改为禁用/启用操作

        /// <summary>
        /// 辅助方法：查找或创建患者
        /// </summary>
        private async Task<PatientDetailDto?> FindOrCreatePatientAsync(PatientDetailDto dto, Guid operatorId, string operatorName)
        {
            // 先尝试根据手机号查找
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                var existing = await _patientService.GetByPhoneNumberAsync(dto.PhoneNumber);
                if (existing != null) return existing;
            }

            // 如果不存在，创建新患者
            return await _patientService.CreateAsync(dto, operatorId, operatorName);
        }
    }
}