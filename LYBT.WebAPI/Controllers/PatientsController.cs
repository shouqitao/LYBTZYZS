using LYBT.Common.Models;
using LYBT.Module.Patients.Dtos;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Records.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 病人管理API接口
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientsController : ControllerBase {
        private readonly IPatientService _patientService;

        public PatientsController(IPatientService patientService) {
            _patientService = patientService;
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
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] PatientDetailDto dto) {
            var (operatorId, operatorName) = GetOperator();
            var result = await _patientService.AddAsync(dto, operatorId, operatorName);
            return result ? Ok() : BadRequest("新增失败，必填项不完整或已存在。");
        }

        /// <summary>
        /// 编辑病人
        /// </summary>
        [HttpPut("edit")]
        public async Task<IActionResult> Edit([FromBody] PatientDetailDto dto) {
            var (operatorId, operatorName) = GetOperator();
            var result = await _patientService.UpdateAsync(dto, operatorId, operatorName);
            return result ? Ok() : BadRequest("更新失败，必填项不完整或病人不存在。");
        }

        [HttpPut("enable/{id}")]
        public async Task<IActionResult> Enable(Guid id) {
            var (operatorId, operatorName) = GetOperator();
            var result = await _patientService.EnableAsync(id, operatorId, operatorName);
            return result ? Ok() : NotFound();
        }

        [HttpPut("disable/{id}")]
        public async Task<IActionResult> Disable(Guid id) {
            var (operatorId, operatorName) = GetOperator();
            var result = await _patientService.DisableAsync(id, operatorId, operatorName);
            return result ? Ok() : NotFound();
        }

        /// <summary>
        /// 获取病人详情
        /// </summary>
        [HttpGet("get/{id}")]
        public async Task<ActionResult<PatientDetailDto>> GetById(Guid id) {
            var data = await _patientService.GetByIdAsync(id);
            return data != null ? Ok(data) : NotFound();
        }

        /// <summary>
        /// 获取全部病人（小数据量场景，分页请用 /paged）
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult<List<PatientDetailDto>>> GetAll() {
            var data = await _patientService.GetAllAsync();
            return Ok(data);
        }

        /// <summary>
        /// 分页条件查询
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<PagedResultDto<PatientDetailDto>>> GetPaged([FromBody] PatientPagedQueryDto query) {
            var result = await _patientService.GetPagedAsync(query);
            return Ok(result);
        }

        /// <summary>
        /// 批量删除病人
        /// </summary>
        [HttpPost("batchDelete")]
        public async Task<IActionResult> BatchDelete([FromBody] List<string> ids) {
            var (operatorId, operatorName) = GetOperator();
            var count = await _patientService.BatchDeleteAsync(ids, operatorId, operatorName);
            return Ok(new { DeletedCount = count });
        }

        [HttpPost("batch-disable")]
        public async Task<IActionResult> BatchDisable([FromBody] BatchIdsDto dto) {
            var (operatorId, operatorName) = GetOperator();
            var count = await _patientService.BatchDisableAsync(dto.Ids, operatorId, operatorName);
            return Ok(new { DisabledCount = count });
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<PatientDetailDto>>> Search([FromQuery] string keyword) {
            var list = await _patientService.SearchAsync(keyword);
            return Ok(list);
        }

        [HttpGet("doctor/{doctorId}")]
        public async Task<ActionResult<List<PatientDetailDto>>> GetForDoctor(Guid doctorId) {
            var list = await _patientService.GetForDoctorAsync(doctorId);
            return Ok(list);
        }

        [HttpPost("{id}/assign-doctor")]
        public async Task<IActionResult> AssignDoctor(Guid id, [FromBody] AssignDoctorDto dto) {
            var (operatorId, operatorName) = GetOperator();
            var result = await _patientService.AssignDoctorAsync(id, dto.DoctorId, operatorId, operatorName);
            return result ? Ok() : BadRequest();
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromBody] List<PatientDetailDto> dtos) {
            var (operatorId, operatorName) = GetOperator();
            var count = await _patientService.ImportAsync(dtos, operatorId, operatorName);
            return Ok(new { Imported = count });
        }

        [HttpPost("export")]
        public async Task<ActionResult<List<PatientDetailDto>>> Export() {
            var data = await _patientService.ExportAsync();
            return Ok(data);
        }

        [HttpGet("{id}/records")]
        public async Task<ActionResult<List<RecordDto>>> GetHistory(Guid id) {
            var data = await _patientService.GetHistoryRecordsAsync(id);
            return Ok(data);
        }
    }
}