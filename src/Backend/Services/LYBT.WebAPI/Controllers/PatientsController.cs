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
                var role = Enum.TryParse<UserRole>(roleStr, out var parsedRole) ? parsedRole : UserRole.Staff;
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
            try {
                var result = await _patientService.AddAsync(dto, operatorId, operatorName);
                return result
                    ? Ok(ApiResponse<object>.Success(new { }, "患者档案创建成功"))
                    : BadRequest(ApiResponse<object>.Fail("患者档案创建失败，必填项不完整或已存在"));
            } catch (Exception ex) {
                return BadRequest(ApiResponse<object>.Fail($"患者档案创建失败：{ex.Message}"));
            }
        }

        /// <summary>
        /// 编辑病人
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Edit(Guid id, [FromBody] PatientDetailDto dto) {
            var (operatorId, operatorName, _) = GetOperator();
            dto.Id = id;
            try {
                var result = await _patientService.UpdateAsync(dto, operatorId, operatorName);
                return result
                    ? Ok(ApiResponse<object>.Success(new { }, "患者档案信息更新成功"))
                    : BadRequest(ApiResponse<object>.Fail("患者档案信息更新失败，必填项不完整或患者档案不存在"));
            } catch (Exception ex) {
                return BadRequest(ApiResponse<object>.Fail($"患者档案信息更新失败：{ex.Message}"));
            }
        }

        /// <summary>
        /// 启用患者档案
        /// </summary>
        [HttpPatch("{id}/enable")]
        public async Task<IActionResult> Enable(Guid id) {
            var (operatorId, operatorName, _) = GetOperator();
            var result = await _patientService.EnableAsync(id, operatorId, operatorName);
            return result ? Ok(ApiResponse<object>.Success(new { }, "患者档案已启用")) : NotFound(ApiResponse<object>.Fail("患者档案不存在"));
        }

        /// <summary>
        /// 禁用患者档案（软删除）
        /// </summary>
        [HttpPatch("{id}/disable")]
        public async Task<IActionResult> Disable(Guid id) {
            var (operatorId, operatorName, _) = GetOperator();
            var result = await _patientService.DisableAsync(id, operatorId, operatorName);
            return result ? Ok(ApiResponse<object>.Success(new { }, "患者档案已禁用")) : NotFound(ApiResponse<object>.Fail("患者档案不存在"));
        }

        /// <summary>
        /// 获取病人详情
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<PatientDetailDto>>> GetById(Guid id) {
            var (_, _, operatorRole) = GetOperator();

            if (!_cache.TryGetValue($"patient:{id}:{operatorRole}", out PatientDetailDto? data)) {
                data = await _patientService.GetByIdAsync(id, operatorRole);
                if (data != null)
                    _cache.Set($"patient:{id}:{operatorRole}", data, TimeSpan.FromMinutes(5));
            }
            return data != null
                ? Ok(ApiResponse<PatientDetailDto>.Success(data))
                : NotFound(ApiResponse<object>.Fail("患者档案不存在或无权限访问"));
        }

        /// <summary>
        /// 获取全部病人（小数据量场景，分页请用 /paged）
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<PatientDetailDto>>>> GetAll() {
            var (_, _, operatorRole) = GetOperator();

            if (!_cache.TryGetValue($"patients:all:{operatorRole}", out List<PatientDetailDto>? data)) {
                data = await _patientService.GetAllAsync(operatorRole);
                _cache.Set($"patients:all:{operatorRole}", data, TimeSpan.FromMinutes(5));
            }
            return Ok(ApiResponse<List<PatientDetailDto>>.Success(data ?? new List<PatientDetailDto>()));
        }

        /// <summary>
        /// 分页条件查询
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<PatientDetailDto>>>> GetPaged([FromBody] PatientPagedQueryDto query) {
            var (_, _, operatorRole) = GetOperator();
            var result = await _patientService.GetPagedAsync(query, operatorRole);
            return Ok(ApiResponse<PaginatedResult<PatientDetailDto>>.Success(result));
        }

        /// <summary>
        /// 批量禁用患者档案
        /// </summary>
        [HttpPatch("batch-disable")]
        public async Task<IActionResult> BatchDisable([FromBody] BatchOperationDto dto) {
            var (operatorId, operatorName, _) = GetOperator();
            var count = await _patientService.BatchDisableAsync(dto.Ids, operatorId, operatorName);
            return Ok(ApiResponse<object>.Success(new { DisabledCount = count, Message = $"成功禁用 {count} 名患者档案" }));
        }

        /// <summary>
        /// 批量启用患者档案
        /// </summary>
        [HttpPatch("batch-enable")]
        public async Task<IActionResult> BatchEnable([FromBody] BatchOperationDto dto) {
            var (operatorId, operatorName, _) = GetOperator();
            var count = await _patientService.BatchEnableAsync(dto.Ids, operatorId, operatorName);
            return Ok(ApiResponse<object>.Success(new { EnabledCount = count, Message = $"成功启用 {count} 名患者档案" }));
        }

        /// <summary>
        /// 搜索患者档案
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<List<PatientDetailDto>>>> Search([FromQuery] string keyword = "") {
            var (_, _, operatorRole) = GetOperator();
            var list = await _patientService.SearchAsync(keyword, operatorRole);
            return Ok(ApiResponse<List<PatientDetailDto>>.Success(list));
        }

        /// <summary>
        /// 导入患者档案数据
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> Import([FromBody] List<PatientDetailDto> dtos) {
            var (operatorId, operatorName, _) = GetOperator();
            var count = await _patientService.ImportAsync(dtos, operatorId, operatorName);
            return Ok(ApiResponse<object>.Success(new { Imported = count, Message = $"成功导入 {count} 名患者档案" }));
        }

        /// <summary>
        /// 导出患者档案数据
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        [HttpGet("export")]
        public async Task<ActionResult<ApiResponse<List<PatientDetailDto>>>> Export() {
            var (_, _, operatorRole) = GetOperator();
            var data = await _patientService.ExportAsync(operatorRole);
            return Ok(ApiResponse<List<PatientDetailDto>>.Success(data));
        }

        /// <summary>
        /// 获取患者档案历史病历
        /// </summary>
        [HttpGet("{id}/records")]
        public async Task<ActionResult<ApiResponse<List<RecordDto>>>> GetHistory(Guid id) {
            var data = await _patientService.GetHistoryRecordsAsync(id);
            return Ok(ApiResponse<List<RecordDto>>.Success(data));
        }

        /// <summary>
        /// 获取启用的患者档案列表
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<ApiResponse<List<PatientDetailDto>>>> GetActivePatients() {
            var patients = await _patientService.GetActivePatientsAsync();
            return Ok(ApiResponse<List<PatientDetailDto>>.Success(patients));
        }

        /// <summary>
        /// 查询或创建患者档案（用于挂号/看诊场景）
        /// 根据姓名和身份证号查询患者档案，如果不存在则创建新档案
        /// </summary>
        [HttpPost("find-or-create")]
        public async Task<ActionResult<ApiResponse<PatientDetailDto>>> FindOrCreate([FromBody] PatientDetailDto dto) {
            var (operatorId, operatorName, _) = GetOperator();
            try {
                var patient = await _patientService.FindOrCreateAsync(dto, operatorId, operatorName);
                return Ok(ApiResponse<PatientDetailDto>.Success(patient, patient.Id == Guid.Empty ? "患者档案创建成功" : "患者档案查询成功"));
            } catch (Exception ex) {
                return BadRequest(ApiResponse<object>.Fail($"患者档案查询或创建失败：{ex.Message}"));
            }
        }

        // 注意：不提供删除接口，患者档案只能禁用，不能删除
        // 原有的删除相关接口已移除，改为禁用/启用操作
    }
}