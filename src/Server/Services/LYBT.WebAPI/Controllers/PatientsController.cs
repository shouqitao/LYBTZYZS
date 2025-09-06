using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 患者管理 API - 统一API响应格式
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class PatientsController : BaseApiController {
        private readonly IPatientService _service;

        public PatientsController(IPatientService service, IMemoryCache cache, ILogger<PatientsController> logger)
            : base(logger, cache) {
            _service = service;
        }

        /// <summary>
        /// 获取患者列表 - 支持分页和查询
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<PatientDto>>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? name = null,
            [FromQuery] string? phone = null,
            [FromQuery] string? idCard = null,
            [FromQuery] bool? isActive = null) {
            try {
                if (page <= 0 || pageSize <= 0 || pageSize > 100) {
                    return ValidationFailPaged<PatientDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var query = new PatientPagedQueryDto {
                    PageIndex = page,
                    PageSize = pageSize,
                    Keyword = keyword,
                    Name = name,
                    PhoneNumber = phone, // 使用正确的属性名
                    IDNumber = idCard    // 使用正确的属性名
                    // 注意：IsActive属性在DTO中不存在，删除该字段
                };

                var result = await _service.GetPagedAsync(query);
                return HandlePagedServiceResult(result, "查询成功");
            } catch (Exception ex) {
                return HandleExceptionPaged<PatientDto>(ex, "获取患者列表", new { page, pageSize, keyword });
            }
        }

        /// <summary>
        /// 获取患者详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<PatientDto>>> GetById(Guid id) {
            try {
                var validationResult = ValidateGuid<PatientDto>(id, "患者ID");
                if (validationResult != null) {
                    return validationResult;
                }

                var result = await _service.GetByIdAsync(id);
                if (!result.IsSuccess || result.Data == null) {
                    return NotFound<PatientDto>(result.ErrorMessage ?? "患者不存在", ApiErrorCodes.PATIENT_NOT_FOUND);
                }

                return Success(result.Data, "查询成功");
            } catch (Exception ex) {
                return HandleException<PatientDto>(ex, "获取患者详情", id);
            }
        }

        /// <summary>
        /// 新增患者
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<PatientDto>>> Add([FromBody] PatientCreateDto dto) {
            try {
                var validationResult = ValidateModel<PatientDto>();
                if (validationResult != null) {
                    return validationResult;
                }

                var result = await _service.CreateAsync(dto);
                if (!result.IsSuccess || result.Data == null) {
                    return BusinessFail<PatientDto>(result.ErrorMessage ?? "新增患者失败", ApiErrorCodes.DATA_SAVE_FAILED);
                }

                LogOperation("新增患者成功", result.Data, result.Data.Id);
                return Success(result.Data, "患者创建成功");
            } catch (Exception ex) {
                return HandleException<PatientDto>(ex, "新增患者", dto);
            }
        }

        /// <summary>
        /// 更新患者信息
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<PatientDto>>> Update(Guid id, [FromBody] PatientUpdateDto dto) {
            try {
                var idValidation = ValidateGuid<PatientDto>(id, "患者ID");
                if (idValidation != null) {
                    return idValidation;
                }

                var modelValidation = ValidateModel<PatientDto>();
                if (modelValidation != null) {
                    return modelValidation;
                }

                var result = await _service.UpdateAsync(id, dto);
                if (!result.IsSuccess || result.Data == null) {
                    return BusinessFail<PatientDto>(result.ErrorMessage ?? "更新患者失败", ApiErrorCodes.DATA_UPDATE_FAILED);
                }

                LogOperation("更新患者成功", result.Data, id);
                return Success(result.Data, "患者更新成功");
            } catch (Exception ex) {
                return HandleException<PatientDto>(ex, "更新患者", new { id, dto });
            }
        }

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "患者ID");
                if (validationResult != null) {
                    return validationResult;
                }

                var result = await _service.DeleteAsync(id);
                if (!result.IsSuccess || !result.Data) {
                    return NotFound("患者不存在", ApiErrorCodes.PATIENT_NOT_FOUND);
                }

                LogOperation("删除患者成功", null, id);
                return Success("删除成功");
            } catch (Exception ex) {
                return HandleException(ex, "删除患者", id);
            }
        }

        /// <summary>
        /// 启用患者
        /// </summary>
        [HttpPost("{id}/enable")]
        public async Task<ActionResult<ApiResponse>> Enable(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "患者ID");
                if (validationResult != null) {
                    return validationResult;
                }

                var result = await _service.EnableAsync(id);
                if (!result.IsSuccess) {
                    return BusinessFail(result.ErrorMessage ?? "启用患者失败", ApiErrorCodes.DATA_UPDATE_FAILED);
                }

                LogOperation("启用患者成功", null, id);
                return Success("启用成功");
            } catch (Exception ex) {
                return HandleException(ex, "启用患者", id);
            }
        }

        /// <summary>
        /// 禁用患者
        /// </summary>
        [HttpPost("{id}/disable")]
        public async Task<ActionResult<ApiResponse>> Disable(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "患者ID");
                if (validationResult != null) {
                    return validationResult;
                }

                var result = await _service.DisableAsync(id);
                if (!result.IsSuccess) {
                    return BusinessFail(result.ErrorMessage ?? "禁用患者失败", ApiErrorCodes.DATA_UPDATE_FAILED);
                }

                LogOperation("禁用患者成功", null, id);
                return Success("禁用成功");
            } catch (Exception ex) {
                return HandleException(ex, "禁用患者", id);
            }
        }

        /// <summary>
        /// 根据身份证号查找患者
        /// </summary>
        [HttpGet("by-idcard/{idCard}")]
        public async Task<ActionResult<ApiResponse<PatientDto>>> GetByIdCard(string idCard) {
            try {
                if (string.IsNullOrWhiteSpace(idCard)) {
                    return ValidationFail<PatientDto>("身份证号不能为空");
                }

                var result = await _service.GetByIdCardAsync(idCard);
                if (!result.IsSuccess || result.Data == null) {
                    return NotFound<PatientDto>(result.ErrorMessage ?? "未找到对应患者", ApiErrorCodes.PATIENT_NOT_FOUND);
                }

                return Success(result.Data, "查询成功");
            } catch (Exception ex) {
                return HandleException<PatientDto>(ex, "根据身份证查找患者", idCard);
            }
        }

        /// <summary>
        /// 根据电话号码查找患者
        /// </summary>
        [HttpGet("by-phone/{phone}")]
        public async Task<ActionResult<ApiResponse<List<PatientDto>>>> GetByPhone(string phone) {
            try {
                if (string.IsNullOrWhiteSpace(phone)) {
                    return ValidationFail<List<PatientDto>>("电话号码不能为空");
                }

                var result = await _service.GetByPhoneAsync(phone);
                if (!result.IsSuccess || result.Data == null) {
                    return Success(new List<PatientDto>(), "未找到匹配患者");
                }

                return Success(result.Data, "查询成功");
            } catch (Exception ex) {
                return HandleException<List<PatientDto>>(ex, "根据电话查找患者", phone);
            }
        }

        /// <summary>
        /// 搜索患者
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<List<PatientDto>>>> Search([FromQuery] string keyword) {
            try {
                if (string.IsNullOrWhiteSpace(keyword)) {
                    return ValidationFail<List<PatientDto>>("搜索关键词不能为空");
                }

                var result = await _service.SearchAsync(keyword);
                if (!result.IsSuccess || result.Data == null) {
                    return Success(new List<PatientDto>(), "未找到匹配患者");
                }

                return Success(result.Data, "搜索成功");
            } catch (Exception ex) {
                return HandleException<List<PatientDto>>(ex, "搜索患者", keyword);
            }
        }

        #region Excel导入导出功能 - 标准功能完善PRD必需功能

        /// <summary>
        /// 批量导入患者
        /// </summary>
        [HttpPost("import")]
        public async Task<ActionResult<ApiResponse<object>>> ImportPatients([FromBody] List<PatientCreateDto> patients) {
            try {
                if (patients == null || patients.Count == 0) {
                    return ValidationFail<object>("导入数据不能为空");
                }

                // 数据验证
                var invalidItems = ValidateImportDataPrivate(patients);
                if (invalidItems.Any()) {
                    return ValidationFail<object>($"存在 {invalidItems.Count} 条无效数据");
                }

                var result = await _service.ImportPatientsAsync(patients);
                if (!result.IsSuccess) {
                    return BusinessFail<object>(result.ErrorMessage ?? "导入患者失败", ApiErrorCodes.DATA_SAVE_FAILED);
                }

                var importResult = new {
                    imported = patients.Count,
                    total = patients.Count,
                    message = $"成功导入 {patients.Count} 个患者"
                };

                LogOperation("批量导入患者成功", importResult, null);
                return Success<object>(importResult, "导入成功");
            } catch (Exception ex) {
                return HandleException<object>(ex, "批量导入患者", patients);
            }
        }

        /// <summary>
        /// 导出患者数据
        /// </summary>
        [HttpGet("export")]
        public async Task<ActionResult<ApiResponse<byte[]>>> ExportPatients([FromQuery] string? format = "excel") {
            try {
                var query = new PagedQueryBaseDto {
                    PageIndex = 1,
                    PageSize = 10000, // 导出时获取所有数据
                    Keyword = string.Empty
                };

                var result = await _service.ExportPatientsAsync(query);
                if (!result.IsSuccess || result.Data == null) {
                    return BusinessFail<byte[]>(result.ErrorMessage ?? "导出患者数据失败", ApiErrorCodes.DATA_EXPORT_FAILED);
                }

                LogOperation("导出患者数据", new { Size = result.Data.Length, Format = format }, null);
                return Success(result.Data, "导出成功");
            } catch (Exception ex) {
                return HandleException<byte[]>(ex, "导出患者数据", format);
            }
        }

        /// <summary>
        /// 导出患者导入模板
        /// </summary>
        [HttpGet("export-template")]
        public ActionResult<ApiResponse<object>> ExportImportTemplate() {
            try {
                var template = new {
                    name = "患者姓名",
                    gender = "男/女",
                    birthDate = "1990-01-01",
                    phoneNumber = "手机号码",
                    idNumber = "身份证号码",
                    address = "联系地址",
                    emergencyContact = "紧急联系人",
                    emergencyPhone = "紧急联系人电话",
                    allergyHistory = "过敏史",
                    medicalHistory = "病史",
                    remark = "备注"
                };

                var templateData = new List<object> { template };

                LogOperation("导出患者导入模板", null, null);
                return Success<object>(new {
                    message = "患者导入模板",
                    template = templateData,
                    instructions = new[]
                    {
                        "请按照模板格式填写患者信息",
                        "姓名和手机号码为必填项",
                        "性别请填写：男 或 女",
                        "日期格式：YYYY-MM-DD",
                        "身份证号码必须为18位"
                    }
                }, "模板导出成功");
            } catch (Exception ex) {
                return HandleException<object>(ex, "导出患者导入模板", null);
            }
        }

        /// <summary>
        /// 验证导入数据有效性
        /// </summary>
        [HttpPost("validate-import")]
        public ActionResult<ApiResponse<object>> ValidateImportData([FromBody] List<PatientCreateDto> patients) {
            try {
                if (patients == null || patients.Count == 0) {
                    return ValidationFail<object>("验证数据不能为空");
                }

                var validationResults = ValidateImportDataDetailed(patients);

                var result = new {
                    totalCount = patients.Count,
                    validCount = validationResults.Count(v => v.IsValid),
                    invalidCount = validationResults.Count(v => !v.IsValid),
                    results = validationResults
                };

                return Success<object>(result, "验证完成");
            } catch (Exception ex) {
                return HandleException<object>(ex, "验证导入数据", patients);
            }
        }

        #endregion Excel导入导出功能 - 标准功能完善PRD必需功能

        #region 私有验证方法

        /// <summary>
        /// 验证导入数据（私有方法）
        /// </summary>
        private List<object> ValidateImportDataPrivate(List<PatientCreateDto> patients) {
            var invalidItems = new List<object>();

            for (int i = 0; i < patients.Count; i++) {
                var patient = patients[i];
                var errors = new List<string>();

                // 必填字段验证
                if (string.IsNullOrWhiteSpace(patient.Name)) {
                    errors.Add("患者姓名不能为空");
                }

                if (string.IsNullOrWhiteSpace(patient.PhoneNumber)) {
                    errors.Add("手机号码不能为空");
                } else if (!IsValidPhoneNumber(patient.PhoneNumber)) {
                    errors.Add("手机号码格式不正确");
                }

                // 身份证验证
                if (!string.IsNullOrWhiteSpace(patient.IdNumber) && !IsValidIdCard(patient.IdNumber)) {
                    errors.Add("身份证号码格式不正确");
                }

                // 性别验证
                if (patient.Gender != LYBT.Shared.Models.Enums.Gender.Male &&
                    patient.Gender != LYBT.Shared.Models.Enums.Gender.Female) {
                    errors.Add("性别必须为男或女");
                }

                if (errors.Any()) {
                    invalidItems.Add(new {
                        index = i + 1,
                        name = patient.Name ?? "未知",
                        errors = errors
                    });
                }
            }

            return invalidItems;
        }

        /// <summary>
        /// 详细验证导入数据
        /// </summary>
        private List<ImportValidationResult> ValidateImportDataDetailed(List<PatientCreateDto> patients) {
            var results = new List<ImportValidationResult>();

            for (int i = 0; i < patients.Count; i++) {
                var patient = patients[i];
                var result = new ImportValidationResult {
                    Index = i + 1,
                    Name = patient.Name ?? "未知",
                    IsValid = true,
                    Errors = new List<string>()
                };

                // 执行验证
                if (string.IsNullOrWhiteSpace(patient.Name)) {
                    result.Errors.Add("患者姓名不能为空");
                    result.IsValid = false;
                }

                if (string.IsNullOrWhiteSpace(patient.PhoneNumber)) {
                    result.Errors.Add("手机号码不能为空");
                    result.IsValid = false;
                } else if (!IsValidPhoneNumber(patient.PhoneNumber)) {
                    result.Errors.Add("手机号码格式不正确");
                    result.IsValid = false;
                }

                if (!string.IsNullOrWhiteSpace(patient.IdNumber) && !IsValidIdCard(patient.IdNumber)) {
                    result.Errors.Add("身份证号码格式不正确");
                    result.IsValid = false;
                }

                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// 验证手机号码格式
        /// </summary>
        private bool IsValidPhoneNumber(string phoneNumber) {
            if (string.IsNullOrWhiteSpace(phoneNumber)) {
                return false;
            }

            return phoneNumber.Length == 11 && phoneNumber.All(char.IsDigit) && phoneNumber.StartsWith("1");
        }

        /// <summary>
        /// 验证身份证号码格式
        /// </summary>
        private bool IsValidIdCard(string idCard) {
            if (string.IsNullOrWhiteSpace(idCard)) {
                return false;
            }

            return idCard.Length == 18 && idCard.Take(17).All(char.IsDigit);
        }

        #endregion 私有验证方法

        #region 内部类

        /// <summary>
        /// 导入验证结果
        /// </summary>
        private class ImportValidationResult {
            public int Index { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new();
        }

        #endregion 内部类

        // UltraThink精简：统计功能已废弃 - 小诊所不需要复杂统计分析
    }
}
