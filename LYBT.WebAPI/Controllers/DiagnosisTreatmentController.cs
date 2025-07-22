using LYBT.Module.DiagnosisTreatment.Interfaces;
using LYBT.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using LYBT.Module.DiagnosisTreatment.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.Module.DiagnosisTreatment.Controllers {

    /// <summary>
    /// 诊疗 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
/// <summary>
/// 表示DiagnosisTreatmentController。
/// </summary>
public class DiagnosisTreatmentController : ControllerBase {
        private readonly IDiagnosisTreatmentService _diagnosisTreatmentService;

        /// <summary>
        /// 构造方法，注入诊疗服务
        /// </summary>
        public DiagnosisTreatmentController(IDiagnosisTreatmentService diagnosisTreatmentService) {
            _diagnosisTreatmentService = diagnosisTreatmentService;
        }

        /// <summary>
        /// 获取诊疗列表
        /// </summary>
        [HttpGet]
/// <summary>
/// 执行GetList操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<DiagnosisTreatmentDto>>> GetList() {
            var list = await _diagnosisTreatmentService.GetListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取诊疗详情
        /// </summary>
        [HttpGet("{id}")]
/// <summary>
/// 执行GetById操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<DiagnosisTreatmentDetailDto>> GetById(Guid id) {
            var detail = await _diagnosisTreatmentService.GetByIdAsync(id);
            if (detail == null)
                return NotFound();
            return Ok(detail);
        }

        /// <summary>
        /// 新增诊疗
        /// </summary>
        [HttpPost]
/// <summary>
/// 执行Add操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Add([FromBody] DiagnosisTreatmentCreateDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _diagnosisTreatmentService.AddAsync(dto);
            if (!result)
                return BadRequest("新增诊疗失败");

            return Ok("新增诊疗成功");
        }

        /// <summary>
        /// 编辑诊疗
        /// </summary>
        [HttpPut]
/// <summary>
/// 执行Update操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Update([FromBody] DiagnosisTreatmentEditDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _diagnosisTreatmentService.UpdateAsync(dto);
            if (!result)
                return BadRequest("编辑诊疗失败");

            return Ok("编辑诊疗成功");
        }

        /// <summary>
        /// 删除诊疗
        /// </summary>
        [HttpDelete("{id}")]
/// <summary>
/// 执行Delete操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Delete(Guid id) {
            var result = await _diagnosisTreatmentService.DeleteAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除诊疗成功");
        }
    }
}
