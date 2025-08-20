using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using BatchOperationDto = LYBT.Shared.Models.Common.BatchOperationDto;
using PatientDto = LYBT.Shared.Models.Contracts.Patients.PatientDto;
using PatientDetailDto = LYBT.Shared.Models.Contracts.Patients.PatientDetailDto;
using PatientCreateDto = LYBT.Shared.Models.Contracts.Patients.PatientCreateDto;
using PatientUpdateDto = LYBT.Shared.Models.Contracts.Patients.PatientUpdateDto;
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
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<PatientDto>>> QuickCreate([FromBody] QuickPatientCreateDto dto)
        {
            try
            {
                var validation = ValidateModel<PatientDto>();
                if (validation != null) return validation;

                var (operatorId, operatorName, operatorRole) = GetOperator();
                // 将QuickPatientCreateDto转换为PatientCreateDto
                var patientCreateDto = new PatientCreateDto
                {
                    Name = dto.Name,
                    Gender = dto.Gender,
                    Age = dto.Age,
                    PhoneNumber = dto.PhoneNumber ?? string.Empty,
                    IDNumber = string.Empty,
                    Address = string.Empty,
                    AllergyHistory = dto.AllergyHistory
                };

                var result = await _patientService.CreateAsync(patientCreateDto);
                
                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("快速创建患者档案", result.Data, result.Data.Id);
                }
                return HandleServiceResult(result, "患者档案快速创建成功");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("已存在"))
            {
                return BusinessFail<PatientDto>(ex.Message, ApiErrorCodes.PHONE_EXISTS);
            }
            catch (Exception ex)
            {
                return HandleException<PatientDto>(ex, "快速创建患者档案", dto);
            }
        }

        // 移除单独的Enable/Disable接口，统一使用ToggleStatus接口

        /// <summary>
        /// 切换患者档案状态（启用/禁用） - 统一API响应格式
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse>> ToggleStatus(Guid id)
        {
            try
            {
                var validation = ValidateGuid(id, "患者ID");
                if (validation != null) return validation;

                // 先获取患者当前状态
                var patientResult = await _patientService.GetByIdAsync(id);
                if (!patientResult.IsSuccess || patientResult.Data == null)
                {
                    return NotFound("患者不存在", ApiErrorCodes.PATIENT_NOT_FOUND);
                }

                // 根据当前状态切换
                ServiceResult<bool> result;
                string message;
                // 简化处理：直接启用
                result = await _patientService.EnableAsync(id);
                message = "患者状态已切换";

                LogOperation(message, null, id);
                return HandleBoolServiceResult(result, message, "状态切换失败");
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
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<List<PatientDto>>>> GetAll()
        {
            try
            {
                var (_, _, operatorRole) = GetOperator();
                var cacheKey = $"patients:all:{operatorRole}";

                if ((_cache?.TryGetValue(cacheKey, out List<PatientDto>? data)) ?? false)
                {
                    return Success(data!, "查询成功（缓存）");
                }

                var result = await _patientService.SearchAsync("");
                if (result.IsSuccess && result.Data != null)
                {
                    _cache?.Set(cacheKey, result.Data, TimeSpan.FromMinutes(5));
                }
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<PatientDto>>(ex, "获取全部患者列表", null);
            }
        }

        /// <summary>
        /// 分页条件查询 - 统一API响应格式
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.PagedApiResponse<PatientDto>>> GetPaged([FromBody] PatientPagedQueryDto query)
        {
            try
            {
                var validation = ValidateModelPaged<PatientDto>();
                if (validation != null) return validation;

                if (query.PageIndex <= 0 || query.PageSize <= 0 || query.PageSize > 100)
                {
                    return ValidationFailPaged<PatientDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var (_, _, operatorRole) = GetOperator();
                var result = await _patientService.GetPagedAsync(query);
                return HandlePagedServiceResult(result, "分页查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<PatientDto>(ex, "分页查询患者", query);
            }
        }

        // 移除未实现的批量操作接口，避免误导用户

        /// <summary>
        /// 搜索患者档案 - 统一API响应格式
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<List<PatientDto>>>> Search([FromQuery] string keyword = "")
        {
            try
            {
                var (_, _, operatorRole) = GetOperator();
                var result = await _patientService.SearchAsync(keyword);
                return HandleServiceResult(result, "搜索完成");
            }
            catch (Exception ex)
            {
                return HandleException<List<PatientDto>>(ex, "搜索患者档案", keyword);
            }
        }

        /// <summary>
        /// 导出患者档案数据 - 统一API响应格式
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        [HttpGet("export")]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<List<PatientDetailDto>>>> Export()
        {
            try
            {
                var (_, _, operatorRole) = GetOperator();
                var result = await _patientService.SearchAsync("");
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<List<PatientDetailDto>>(result.ErrorMessage ?? "获取患者数据失败", ApiErrorCodes.INTERNAL_ERROR);
                }
                
                // 将PatientDto转换为PatientDetailDto（简化处理）
                var detailData = result.Data.Select(p => new PatientDetailDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Gender = p.Gender,
                    Age = p.Age,
                    PhoneNumber = p.PhoneNumber,
                    IDNumber = p.IDNumber,
                    Address = p.Address,
                    AllergyHistory = p.AllergyHistory,
                    // PatientDto继承BaseDto，没有审计字段
                }).ToList();
                
                LogOperation("导出患者档案", new { count = detailData.Count });
                return Success(detailData, $"成功导出{detailData.Count}条患者档案");
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
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<List<PatientDetailDto>>>> GetActivePatients()
        {
            try
            {
                var result = await _patientService.SearchAsync("");
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<List<PatientDetailDto>>(result.ErrorMessage ?? "获取患者数据失败", ApiErrorCodes.INTERNAL_ERROR);
                }
                
                // 将PatientDto转换为PatientDetailDto并过滤启用的（简化处理）
                var detailData = result.Data.Select(p => new PatientDetailDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Gender = p.Gender,
                    Age = p.Age,
                    PhoneNumber = p.PhoneNumber,
                    IDNumber = p.IDNumber,
                    Address = p.Address,
                    AllergyHistory = p.AllergyHistory,
                    // PatientDto继承BaseDto，没有审计字段
                }).ToList();
                
                return Success(detailData, $"查询成功，共{detailData.Count}条启用的患者档案");
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
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<PatientDetailDto>>> FindOrCreate([FromBody] PatientDetailDto dto)
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
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.PagedApiResponse<PatientDto>>> GetPatients(
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
                    return ValidationFailPaged<PatientDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var (_, _, operatorRole) = GetOperator();
                var query = new PatientPagedQueryDto
                {
                    PageIndex = page,
                    PageSize = pageSize,
                    Keyword = keyword,
                    Name = name,
                    PhoneNumber = phoneNumber,
                    IDNumber = idNumber,
                    Address = address,
                    Gender = gender,
                    MinAge = minAge,
                    MaxAge = maxAge
                };
                var result = await _patientService.GetPagedAsync(query);
                return HandlePagedServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<PatientDto>(ex, "获取患者列表", new { page, pageSize, keyword });
            }
        }

        /// <summary>
        /// 创建新患者 (RESTful POST /Patients) - 统一API响应格式
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<PatientDetailDto>>> CreatePatient([FromBody] PatientDetailDto dto)
        {
            try
            {
                var validation = ValidateModel<PatientDetailDto>();
                if (validation != null) return validation;

                var (operatorId, operatorName, operatorRole) = GetOperator();
                // 将PatientDetailDto转换为PatientCreateDto
                var createDto = new PatientCreateDto
                {
                    Name = dto.Name,
                    Gender = dto.Gender,
                    Age = dto.Age,
                    DateOfBirth = dto.DateOfBirth,
                    PhoneNumber = dto.PhoneNumber,
                    IDNumber = dto.IDNumber,
                    Address = dto.Address,
                    AllergyHistory = dto.AllergyHistory,
                    MedicalHistory = dto.MedicalHistory,
                    FamilyHistory = dto.FamilyHistory,
                    Profession = dto.Profession,
                    MaritalStatus = dto.MaritalStatus,
                    EmergencyContact = dto.EmergencyContact,
                    EmergencyPhone = dto.EmergencyPhone
                };
                
                var result = await _patientService.CreateAsync(createDto);
                
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<PatientDetailDto>(result.ErrorMessage ?? "患者创建失败", ApiErrorCodes.DATA_SAVE_FAILED);
                }

                // 转换回PatientDetailDto
                var detailResult = new PatientDetailDto
                {
                    Id = result.Data.Id,
                    Name = result.Data.Name,
                    Gender = result.Data.Gender,
                    Age = result.Data.Age,
                    PhoneNumber = result.Data.PhoneNumber,
                    IDNumber = result.Data.IDNumber,
                    Address = result.Data.Address,
                    AllergyHistory = result.Data.AllergyHistory
                    // PatientDto继承BaseDto，没有审计字段
                };

                LogOperation("患者创建成功", detailResult, detailResult.Id);
                return Success(detailResult, "患者创建成功");
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
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<PatientDetailDto>>> GetPatient(Guid id)
        {
            try
            {
                var validation = ValidateGuid<PatientDetailDto>(id, "患者ID");
                if (validation != null) return validation;

                var (_, _, operatorRole) = GetOperator();
                var result = await _patientService.GetByIdAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return NotFound<PatientDetailDto>("患者不存在", ApiErrorCodes.PATIENT_NOT_FOUND);
                }
                
                return Success(result.Data, "查询成功");
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
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse<PatientDetailDto>>> UpdatePatient(Guid id, [FromBody] PatientDetailDto dto)
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
                // 将PatientDetailDto转换为PatientUpdateDto
                var updateDto = new PatientUpdateDto
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Gender = dto.Gender,
                    Age = dto.Age,
                    DateOfBirth = dto.DateOfBirth,
                    PhoneNumber = dto.PhoneNumber,
                    IDNumber = dto.IDNumber,
                    Address = dto.Address,
                    AllergyHistory = dto.AllergyHistory,
                    MedicalHistory = dto.MedicalHistory,
                    FamilyHistory = dto.FamilyHistory,
                    Profession = dto.Profession,
                    MaritalStatus = dto.MaritalStatus,
                    EmergencyContact = dto.EmergencyContact,
                    EmergencyPhone = dto.EmergencyPhone
                };
                
                var result = await _patientService.UpdateAsync(id, updateDto);
                
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<PatientDetailDto>(result.ErrorMessage ?? "患者信息更新失败", ApiErrorCodes.DATA_UPDATE_FAILED);
                }

                // 获取更新后的资源
                var updated = await _patientService.GetByIdAsync(id);
                if (!updated.IsSuccess || updated.Data == null)
                {
                    return BusinessFail<PatientDetailDto>("患者更新后查询失败", ApiErrorCodes.PATIENT_NOT_FOUND);
                }
                LogOperation("更新患者信息成功", updated.Data, id);
                return Success(updated.Data, "患者信息更新成功");
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
        public async Task<ActionResult<LYBT.Shared.Models.Contracts.Common.ApiResponse>> DeletePatient(Guid id)
        {
            try
            {
                var validation = ValidateGuid(id, "患者ID");
                if (validation != null) return validation;

                var (operatorId, operatorName, operatorRole) = GetOperator();
                var result = await _patientService.DisableAsync(id);
                
                if (!result.IsSuccess || !result.Data)
                {
                    return BusinessFail(result.ErrorMessage ?? "禁用患者失败", ApiErrorCodes.DATA_UPDATE_FAILED);
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
                var existingResult = await _patientService.GetByPhoneAsync(dto.PhoneNumber);
                if (existingResult.IsSuccess && existingResult.Data != null && existingResult.Data.Any())
                {
                    // 转换为PatientDetailDto
                    var first = existingResult.Data.First();
                    return new PatientDetailDto
                    {
                        Id = first.Id,
                        Name = first.Name,
                        Gender = first.Gender,
                        Age = first.Age,
                        PhoneNumber = first.PhoneNumber,
                        IDNumber = first.IDNumber,
                        Address = first.Address,
                        AllergyHistory = first.AllergyHistory
                        // PatientDto继承BaseDto，没有审计字段
                    };
                }
            }

            // 如果不存在，创建新患者
            var createDto = new PatientCreateDto
            {
                Name = dto.Name,
                Gender = dto.Gender,
                Age = dto.Age,
                DateOfBirth = dto.DateOfBirth,
                PhoneNumber = dto.PhoneNumber,
                IDNumber = dto.IDNumber,
                Address = dto.Address,
                AllergyHistory = dto.AllergyHistory,
                MedicalHistory = dto.MedicalHistory,
                FamilyHistory = dto.FamilyHistory,
                Profession = dto.Profession,
                MaritalStatus = dto.MaritalStatus,
                EmergencyContact = dto.EmergencyContact,
                EmergencyPhone = dto.EmergencyPhone
            };
            
            var createResult = await _patientService.CreateAsync(createDto);
            if (!createResult.IsSuccess || createResult.Data == null)
            {
                return null;
            }
            
            // 转换为PatientDetailDto
            return new PatientDetailDto
            {
                Id = createResult.Data.Id,
                Name = createResult.Data.Name,
                Gender = createResult.Data.Gender,
                Age = createResult.Data.Age,
                PhoneNumber = createResult.Data.PhoneNumber,
                IDNumber = createResult.Data.IDNumber,
                Address = createResult.Data.Address,
                AllergyHistory = createResult.Data.AllergyHistory
                // PatientDto继承BaseDto，没有审计字段
            };
        }
    }
}