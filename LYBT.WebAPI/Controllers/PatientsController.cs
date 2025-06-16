using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Patients.Interfaces;
using LYBT.Common.Models;
using LYBT.Module.Patients.Dtos;

namespace LYBT.WebAPI.Controllers {
    /// <summary>
    /// 病人管理API接口
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase {
        private readonly IPatientService _patientService;

        public PatientsController(IPatientService patientService) {
            _patientService = patientService;
        }

        /// <summary>
        /// 新增病人
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] PatientCreateDto dto) {
            Guid operatorId = Guid.NewGuid();
            string operatorName = "管理员A";
            var result = await _patientService.AddAsync(dto, operatorId, operatorName);
            return result ? Ok() : BadRequest("新增失败，必填项不完整或已存在。");
        }

        /// <summary>
        /// 编辑病人
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> Edit([FromBody] PatientEditDto dto) {
            Guid operatorId = Guid.NewGuid();
            string operatorName = "管理员A";
            var result = await _patientService.UpdateAsync(dto, operatorId, operatorName);
            return result ? Ok() : BadRequest("更新失败，必填项不完整或病人不存在。");
        }

        /// <summary>
        /// 删除单个病人
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id) {
            Guid operatorId = Guid.NewGuid();
            string operatorName = "管理员A";
            var result = await _patientService.DeleteAsync(id, operatorId, operatorName);
            return result ? Ok() : NotFound("指定病人不存在。");
        }

        /// <summary>
        /// 获取病人详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PatientDetailDto>> GetById(Guid id) {
            var data = await _patientService.GetByIdAsync(id);
            return data != null ? Ok(data) : NotFound();
        }

        /// <summary>
        /// 获取全部病人（小数据量场景，分页请用 /paged）
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult<List<PatientDto>>> GetAll() {
            var data = await _patientService.GetAllAsync();
            return Ok(data);
        }

        /// <summary>
        /// 分页条件查询
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<PagedResultDto<PatientDto>>> GetPaged([FromBody] PatientPagedQueryDto query) {
            var result = await _patientService.GetPagedAsync(query);
            return Ok(result);
        }

        /// <summary>
        /// 批量删除病人
        /// </summary>
        [HttpPost("batchDelete")]
        public async Task<IActionResult> BatchDelete([FromBody] List<string> ids) {
            Guid operatorId = Guid.NewGuid();
            string operatorName = "管理员A";
            var count = await _patientService.BatchDeleteAsync(ids, operatorId, operatorName);
            return Ok(new { DeletedCount = count });
        }
    }
}
