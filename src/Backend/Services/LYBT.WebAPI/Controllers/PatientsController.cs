using Asp.Versioning;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Records;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using BatchOperationDto = LYBT.Shared.Models.Common.BatchOperationDto;
using PatientDetailDto = LYBT.Shared.Models.Contracts.Patients.PatientDetailDto;
using PatientPagedQueryDto = LYBT.Shared.Models.Contracts.Patients.PatientPagedQueryDto;
using QuickPatientCreateDto = LYBT.Shared.Models.Contracts.Patients.QuickPatientCreateDto;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 病人管理API接口
    /// 实现软删除策略：患者档案档案只能禁用/启用，不提供删除接口
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class PatientsController : ControllerBase {
        private readonly IPatientService _patientService;
        private readonly IMemoryCache _cache;

        public PatientsController(IPatientService patientService, IMemoryCache cache) {
            _patientService = patientService;
            _cache = cache;
        }

        /// <summary>
        /// 获取当前操作者信息
        /// </summary>
        private (Guid operatorId, string operatorName, UserRole operatorRole) GetOperator() {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User?.Identity?.Name;
            var roleStr = User?.FindFirst(ClaimTypes.Role)?.Value;

            if (Guid.TryParse(userId, out var opId) && !string.IsNullOrEmpty(userName)) {
                var role = Enum.TryParse<UserRole>(roleStr, out var parsedRole) ? parsedRole : UserRole.RegistrationStaff;
                return (opId, userName, role);
            }
            throw new UnauthorizedAccessException("未登录或用户信息无效");
        }

        /// <summary>
        /// 新增病人
        /// </summary>
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] PatientDetailDto dto) {
            var (operatorId, operatorName, _) = GetOperator();
            var result = await _patientService.AddAsync(dto, operatorId, operatorName);
            if (result) {
                return Ok(new { message = "患者档案创建成功" });
            } else {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = "患者档案创建失败，必填项不完整或已存在",
                    Status = 400
                });
            }
        }

        /// <summary>
        /// 快速创建患者档案（简化版本）
        /// </summary>
        [HttpPost("quick")]
        public async Task<IActionResult> QuickCreate([FromBody] QuickPatientCreateDto dto) {
            var (operatorId, operatorName, _) = GetOperator();
            // 将QuickPatientCreateDto转换为PatientDetailDto
            var patientDto = new PatientDetailDto {
                Name = dto.Name,
                Gender = dto.Gender,
                Age = dto.Age ?? 0,
                PhoneNumber = dto.PhoneNumber ?? dto.Phone ?? string.Empty,
                IDNumber = dto.IDNumber ?? string.Empty,
                Address = dto.Address ?? string.Empty
            };

            var result = await _patientService.AddAsync(patientDto, operatorId, operatorName);
            if (result) {
                return Ok(new { message = "患者档案快速创建成功" });
            } else {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = "患者档案快速创建失败，必填项不完整或已存在",
                    Status = 400
                });
            }
        }


        /// <summary>
        /// 启用患者档案
        /// </summary>
        [HttpPatch("{id}/enable")]
        public async Task<IActionResult> Enable(Guid id) {
            var (operatorId, operatorName, _) = GetOperator();
            var result = await _patientService.EnableAsync(id, operatorId, operatorName);
            if (result) {
                return Ok(new { message = "患者档案已启用" });
            } else {
                return NotFound(new ProblemDetails {
                    Title = "资源未找到",
                    Detail = "患者档案不存在",
                    Status = 404
                });
            }
        }

        /// <summary>
        /// 禁用患者档案（软删除）
        /// </summary>
        [HttpPatch("{id}/disable")]
        public async Task<IActionResult> Disable(Guid id) {
            var (operatorId, operatorName, _) = GetOperator();
            var result = await _patientService.DisableAsync(id, operatorId, operatorName);
            if (result) {
                return Ok(new { message = "患者档案已禁用" });
            } else {
                return NotFound(new ProblemDetails {
                    Title = "资源未找到",
                    Detail = "患者档案不存在",
                    Status = 404
                });
            }
        }

        /// <summary>
        /// 切换患者档案状态（启用/禁用）
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(Guid id) {
            var (operatorId, operatorName, operatorRole) = GetOperator();
            // 先获取患者当前状态
            var patient = await _patientService.GetByIdAsync(id, operatorRole);
            if (patient == null) {
                return NotFound(new ProblemDetails {
                    Title = "资源未找到",
                    Detail = "患者档案不存在",
                    Status = 404
                });
            }

            // 根据当前状态切换
            bool result;
            string message;
            if (patient.IsActive) {
                result = await _patientService.DisableAsync(id, operatorId, operatorName);
                message = "患者档案已禁用";
            } else {
                result = await _patientService.EnableAsync(id, operatorId, operatorName);
                message = "患者档案已启用";
            }
            
            if (result) {
                return Ok(new { message });
            } else {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = "状态切换失败",
                    Status = 400
                });
            }
        }

        /// <summary>
        /// 获取全部病人（小数据量场景，分页请用 /paged）
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult<List<PatientDetailDto>>> GetAll() {
            var (_, _, operatorRole) = GetOperator();

            if (!_cache.TryGetValue($"patients:all:{operatorRole}", out List<PatientDetailDto>? data)) {
                data = await _patientService.GetAllAsync(operatorRole);
                _cache.Set($"patients:all:{operatorRole}", data, TimeSpan.FromMinutes(5));
            }
            return Ok(data ?? new List<PatientDetailDto>());
        }

        /// <summary>
        /// 分页条件查询
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<PaginatedResult<PatientDetailDto>>> GetPaged([FromBody] PatientPagedQueryDto query) {
            var (_, _, operatorRole) = GetOperator();
            var result = await _patientService.GetPagedAsync(query, operatorRole);
            return Ok(result);
        }

        /// <summary>
        /// 批量禁用患者档案
        /// </summary>
        [HttpPatch("batch-disable")]
        public async Task<IActionResult> BatchDisable([FromBody] BatchOperationDto dto) {
            var (operatorId, operatorName, _) = GetOperator();
            var count = await _patientService.BatchDisableAsync(dto.Ids, operatorId, operatorName);
            return Ok(new { disabledCount = count, message = $"成功禁用 {count} 名患者档案" });
        }

        /// <summary>
        /// 批量启用患者档案
        /// </summary>
        [HttpPatch("batch-enable")]
        public async Task<IActionResult> BatchEnable([FromBody] BatchOperationDto dto) {
            var (operatorId, operatorName, _) = GetOperator();
            var count = await _patientService.BatchEnableAsync(dto.Ids, operatorId, operatorName);
            return Ok(new { enabledCount = count, message = $"成功启用 {count} 名患者档案" });
        }

        /// <summary>
        /// 搜索患者档案
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<List<PatientDetailDto>>> Search([FromQuery] string keyword = "") {
            var (_, _, operatorRole) = GetOperator();
            var list = await _patientService.SearchAsync(keyword, operatorRole);
            return Ok(list);
        }

        /// <summary>
        /// 导入患者档案数据
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> Import([FromBody] List<PatientDetailDto> dtos) {
            var (operatorId, operatorName, _) = GetOperator();
            var count = await _patientService.ImportAsync(dtos, operatorId, operatorName);
            return Ok(new { imported = count, message = $"成功导入 {count} 名患者档案" });
        }

        /// <summary>
        /// 导出患者档案数据
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        [HttpGet("export")]
        public async Task<ActionResult<List<PatientDetailDto>>> Export() {
            var (_, _, operatorRole) = GetOperator();
            var data = await _patientService.ExportAsync(operatorRole);
            return Ok(data);
        }

        /// <summary>
        /// 获取患者档案历史病历
        /// </summary>
        [HttpGet("{id}/records")]
        public async Task<ActionResult<List<RecordDto>>> GetHistory(Guid id) {
            var data = await _patientService.GetHistoryRecordsAsync(id);
            return Ok(data);
        }

        /// <summary>
        /// 获取启用的患者档案列表
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<List<PatientDetailDto>>> GetActivePatients() {
            var patients = await _patientService.GetActivePatientsAsync();
            return Ok(patients);
        }

        /// <summary>
        /// 查询或创建患者档案（用于挂号/看诊场景）
        /// 根据姓名和身份证号查询患者档案，如果不存在则创建新档案
        /// </summary>
        [HttpPost("find-or-create")]
        public async Task<ActionResult<PatientDetailDto>> FindOrCreate([FromBody] PatientDetailDto dto) {
            var (operatorId, operatorName, _) = GetOperator();
            var patient = await _patientService.FindOrCreateAsync(dto, operatorId, operatorName);
            return Ok(patient);
        }

        // ======================== RESTful 标准接口 ========================

        /// <summary>
        /// 获取所有患者列表 (RESTful GET /Patients) - 支持多字段模糊查询
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<PatientDetailDto>>> GetPatients(
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
            [FromQuery] PatientStatus? status = null) {
            var (_, _, operatorRole) = GetOperator();
            var query = new PatientPagedQueryDto {
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
            var result = await _patientService.GetPagedAsync(query, operatorRole);
            return Ok(result);
        }

        /// <summary>
        /// 创建新患者 (RESTful POST /Patients)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreatePatient([FromBody] PatientDetailDto dto) {
            var (operatorId, operatorName, _) = GetOperator();
            var result = await _patientService.AddAsync(dto, operatorId, operatorName);
            if (result) {
                return StatusCode(201, new { message = "患者创建成功" });
            } else {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = "患者创建失败",
                    Status = 400
                });
            }
        }

        /// <summary>
        /// 根据ID获取患者 (RESTful GET /Patients/{id})
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PatientDetailDto>> GetPatient(Guid id) {
            var (_, _, operatorRole) = GetOperator();
            var patient = await _patientService.GetByIdAsync(id, operatorRole);
            if (patient == null) {
                return NotFound(new ProblemDetails {
                    Title = "资源未找到",
                    Detail = "患者不存在",
                    Status = 404
                });
            }
            return Ok(patient);
        }

        /// <summary>
        /// 更新患者信息 (RESTful PUT /Patients/{id})
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] PatientDetailDto dto) {
            var (operatorId, operatorName, _) = GetOperator();
            // 确保DTO的ID与路由参数一致
            dto.Id = id;
            var result = await _patientService.UpdateAsync(dto, operatorId, operatorName);
            if (result) {
                return Ok(new { message = "患者信息更新成功" });
            } else {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = "患者信息更新失败",
                    Status = 400
                });
            }
        }

        /// <summary>
        /// 删除患者 (RESTful DELETE /Patients/{id}) - 实际执行软删除
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(Guid id) {
            var (operatorId, operatorName, _) = GetOperator();
            var result = await _patientService.DisableAsync(id, operatorId, operatorName);
            if (result) {
                return Ok(new { message = "患者已禁用" });
            } else {
                return BadRequest(new ProblemDetails {
                    Title = "操作失败",
                    Detail = "禁用患者失败",
                    Status = 400
                });
            }
        }

        // 注意：不提供真正的删除接口，患者档案只能禁用，不能删除
        // 原有的删除相关接口已移除，改为禁用/启用操作
    }
}