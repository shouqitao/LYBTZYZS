using LYBT.Module.Records.Dtos;
using LYBT.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using LYBT.Module.Records.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.Module.Records.Controllers {

    /// <summary>
    /// 病历 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
/// <summary>
/// 表示RecordController。
/// </summary>
public class RecordController : ControllerBase {
        private readonly IRecordService _recordService;

        /// <summary>
        /// 构造方法，注入病历服务
        /// </summary>
        public RecordController(IRecordService recordService) {
            _recordService = recordService;
        }

        /// <summary>
        /// 获取病历列表
        /// </summary>
        [HttpGet]
/// <summary>
/// 执行GetList操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<RecordDto>>> GetList() {
            var list = await _recordService.GetListAsync();
            return Ok(list);
        }

        [HttpGet("patient/{patientId}")]
/// <summary>
/// 执行GetByPatient操作。
/// </summary>
/// <param name="patientId">参数patientId</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<RecordDto>>> GetByPatient(Guid patientId) {
            var list = await _recordService.GetByPatientIdAsync(patientId);
            return Ok(list);
        }

        /// <summary>
        /// 获取病历详情
        /// </summary>
        [HttpGet("{id}")]
/// <summary>
/// 执行GetById操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<RecordDetailDto>> GetById(Guid id) {
            var detail = await _recordService.GetByIdAsync(id);
            if (detail == null)
                return NotFound();
            return Ok(detail);
        }

        /// <summary>
        /// 新增病历
        /// </summary>
        [HttpPost]
/// <summary>
/// 执行Add操作。
/// </summary>
/// <param name="recordCreateDto">参数recordCreateDto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Add([FromBody] RecordCreateDto recordCreateDto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Guid operatorId = Guid.NewGuid();
            string operatorName = "管理员A";
            var result = await _recordService.AddAsync(recordCreateDto, operatorId, operatorName);
            if (!result)
                return BadRequest("新增病历失败");

            return Ok("新增病历成功");
        }

        /// <summary>
        /// 编辑病历
        /// </summary>
        [HttpPut]
/// <summary>
/// 执行Update操作。
/// </summary>
/// <param name="recordEditDto">参数recordEditDto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Update([FromBody] RecordEditDto recordEditDto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Guid operatorId = Guid.NewGuid();
            string operatorName = "管理员A";
            var result = await _recordService.UpdateAsync(recordEditDto, operatorId, operatorName);
            if (!result)
                return BadRequest("编辑病历失败");

            return Ok("编辑病历成功");
        }

        /// <summary>
        /// 删除病历
        /// </summary>
        [HttpDelete("{id}")]
/// <summary>
/// 执行Delete操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Delete(Guid id) {
            Guid operatorId = Guid.NewGuid();
            string operatorName = "管理员A";
            var result = await _recordService.DeleteAsync(id, operatorId, operatorName);
            if (!result)
                return NotFound();
            return Ok("删除病历成功");
        }

        [HttpPost("share/{id}")]
/// <summary>
/// 执行MarkAsShared操作。
/// </summary>
/// <param name="id">参数id</param>
/// <param name="doctorIds">参数doctorIds</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> MarkAsShared(Guid id, [FromBody] List<string> doctorIds) {
            var result = await _recordService.MarkAsSharedAsync(id, doctorIds);
            if (!result)
                return NotFound();
            return Ok();
        }

        [HttpPost("unshare/{id}")]
/// <summary>
/// 执行RevokeSharing操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> RevokeSharing(Guid id) {
            var result = await _recordService.RevokeSharingAsync(id);
            if (!result)
                return NotFound();
            return Ok();
        }

        [HttpGet("shared/{doctorId}")]
/// <summary>
/// 执行GetShared操作。
/// </summary>
/// <param name="doctorId">参数doctorId</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<RecordDto>>> GetShared(Guid doctorId) {
            var list = await _recordService.GetSharedRecordsAsync(doctorId);
            return Ok(list);
        }
    }
}
