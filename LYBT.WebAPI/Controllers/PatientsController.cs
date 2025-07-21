using LYBT.Common.Models;
using LYBT.Common.Responses;
using LYBT.Module.Patients.Dtos;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Records.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 病人管理API接口
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

        private (Guid operatorId, string operatorName) GetOperator() {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User?.Identity?.Name;
            if (Guid.TryParse(userId, out var opId) && !string.IsNullOrEmpty(userName))
                return (opId, userName);
            throw new UnauthorizedAccessException("未登录或用户信息无效");
        }

        /// <summary>
        /// 新增病人
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] PatientDetailDto dto) {
            var (operatorId, operatorName) = GetOperator();
            var result = await _patientService.AddAsync(dto, operatorId, operatorName);
            return result
                ? Ok(ApiResponse<object>.Success(null))
                : BadRequest(ApiResponse<object>.Fail("新增失败，必填项不完整或已存在。"));
        }

        /// <summary>
        /// 编辑病人
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Edit(Guid id, [FromBody] PatientDetailDto dto) {
            var (operatorId, operatorName) = GetOperator();
            dto.Id = id;
            var result = await _patientService.UpdateAsync(dto, operatorId, operatorName);
            return result
                ? Ok(ApiResponse<object>.Success(null))
                : BadRequest(ApiResponse<object>.Fail("更新失败，必填项不完整或病人不存在。"));
        }

        [HttpPatch("{id}/enable")]
        public async Task<IActionResult> Enable(Guid id) {
            var (operatorId, operatorName) = GetOperator();
            var result = await _patientService.EnableAsync(id, operatorId, operatorName);
            return result ? Ok(ApiResponse<object>.Success(null)) : NotFound();
        }

        [HttpPatch("{id}/disable")]
        public async Task<IActionResult> Disable(Guid id) {
            var (operatorId, operatorName) = GetOperator();
            var result = await _patientService.DisableAsync(id, operatorId, operatorName);
            return result ? Ok(ApiResponse<object>.Success(null)) : NotFound();
        }

        /// <summary>
        /// 获取病人详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<PatientDetailDto>>> GetById(Guid id) {
            if (!_cache.TryGetValue($"patient:{id}", out PatientDetailDto? data)) {
                data = await _patientService.GetByIdAsync(id);
                if (data != null)
                    _cache.Set($"patient:{id}", data, TimeSpan.FromMinutes(5));
            }
            return data != null
                ? Ok(ApiResponse<PatientDetailDto>.Success(data))
                : NotFound(ApiResponse<object>.Fail("未找到"));
        }

        /// <summary>
        /// 获取全部病人（小数据量场景，分页请用 /paged）
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<PatientDetailDto>>>> GetAll() {
            if (!_cache.TryGetValue("patients:all", out List<PatientDetailDto>? data)) {
                data = await _patientService.GetAllAsync();
                _cache.Set("patients:all", data, TimeSpan.FromMinutes(5));
            }
            return Ok(ApiResponse<List<PatientDetailDto>>.Success(data));
        }

        /// <summary>
        /// 分页条件查询
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<ApiResponse<PagedResultDto<PatientDetailDto>>>> GetPaged([FromBody] PatientPagedQueryDto query) {
            var result = await _patientService.GetPagedAsync(query);
            return Ok(ApiResponse<PagedResultDto<PatientDetailDto>>.Success(result));
        }

        /// <summary>
        /// 批量删除病人
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> BatchDelete([FromBody] List<string> ids) {
            var (operatorId, operatorName) = GetOperator();
            var count = await _patientService.BatchDeleteAsync(ids, operatorId, operatorName);
            return Ok(ApiResponse<object>.Success(new { DeletedCount = count }));
        }

        [HttpPatch("batch-disable")]
        public async Task<IActionResult> BatchDisable([FromBody] BatchIdsDto dto) {
            var (operatorId, operatorName) = GetOperator();
            var count = await _patientService.BatchDisableAsync(dto.Ids, operatorId, operatorName);
            return Ok(ApiResponse<object>.Success(new { DisabledCount = count }));
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<List<PatientDetailDto>>>> Search([FromQuery] string keyword) {
            var list = await _patientService.SearchAsync(keyword);
            return Ok(ApiResponse<List<PatientDetailDto>>.Success(list));
        }

        [HttpGet("doctor/{doctorId}")]
        public async Task<ActionResult<ApiResponse<List<PatientDetailDto>>>> GetForDoctor(Guid doctorId) {
            var list = await _patientService.GetForDoctorAsync(doctorId);
            return Ok(ApiResponse<List<PatientDetailDto>>.Success(list));
        }

        [HttpPatch("{id}/assign-doctor")]
        public async Task<IActionResult> AssignDoctor(Guid id, [FromBody] AssignDoctorDto dto) {
            var (operatorId, operatorName) = GetOperator();
            var result = await _patientService.AssignDoctorAsync(id, dto.DoctorId, operatorId, operatorName);
            return result ? Ok(ApiResponse<object>.Success(null)) : BadRequest(ApiResponse<object>.Fail("失败"));
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromBody] List<PatientDetailDto> dtos) {
            var (operatorId, operatorName) = GetOperator();
            var count = await _patientService.ImportAsync(dtos, operatorId, operatorName);
            return Ok(ApiResponse<object>.Success(new { Imported = count }));
        }

        [HttpGet("export")]
        public async Task<ActionResult<ApiResponse<List<PatientDetailDto>>>> Export() {
            var data = await _patientService.ExportAsync();
            return Ok(ApiResponse<List<PatientDetailDto>>.Success(data));
        }

        [HttpGet("{id}/records")]
        public async Task<ActionResult<ApiResponse<List<RecordDto>>>> GetHistory(Guid id) {
            var data = await _patientService.GetHistoryRecordsAsync(id);
            return Ok(ApiResponse<List<RecordDto>>.Success(data));
        }
    }
}