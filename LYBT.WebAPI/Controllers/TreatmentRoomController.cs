using LYBT.Module.TreatmentRoom.Dtos;
using LYBT.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using LYBT.Module.TreatmentRoom.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.Module.TreatmentRoom.Controllers {

    /// <summary>
    /// 治疗室 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
/// <summary>
/// 表示TreatmentRoomController。
/// </summary>
public class TreatmentRoomController : ControllerBase {
        private readonly ITreatmentRoomService _treatmentRoomService;

        /// <summary>
        /// 构造方法，注入治疗室服务
        /// </summary>
        public TreatmentRoomController(ITreatmentRoomService treatmentRoomService) {
            _treatmentRoomService = treatmentRoomService;
        }

        /// <summary>
        /// 获取治疗室单列表
        /// </summary>
        [HttpGet]
/// <summary>
/// 执行GetList操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<TreatmentRoomDto>>> GetList() {
            var list = await _treatmentRoomService.GetListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取治疗室单详情
        /// </summary>
        [HttpGet("{id}")]
/// <summary>
/// 执行GetById操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<TreatmentRoomDetailDto>> GetById(Guid id) {
            var detail = await _treatmentRoomService.GetByIdAsync(id);
            if (detail == null)
                return NotFound();
            return Ok(detail);
        }

        /// <summary>
        /// 新增治疗室单
        /// </summary>
        [HttpPost]
/// <summary>
/// 执行Add操作。
/// </summary>
/// <param name="treatmentRoomCreateDto">参数treatmentRoomCreateDto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Add([FromBody] TreatmentRoomCreateDto treatmentRoomCreateDto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _treatmentRoomService.AddAsync(treatmentRoomCreateDto);
            if (!result)
                return BadRequest("新增治疗室单失败");

            return Ok("新增治疗室单成功");
        }

        /// <summary>
        /// 编辑治疗室单
        /// </summary>
        [HttpPut]
/// <summary>
/// 执行Update操作。
/// </summary>
/// <param name="treatmentRoomEditDto">参数treatmentRoomEditDto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Update([FromBody] TreatmentRoomEditDto treatmentRoomEditDto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _treatmentRoomService.UpdateAsync(treatmentRoomEditDto);
            if (!result)
                return BadRequest("编辑治疗室单失败");

            return Ok("编辑治疗室单成功");
        }

        /// <summary>
        /// 删除治疗室单
        /// </summary>
        [HttpDelete("{id}")]
/// <summary>
/// 执行Delete操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Delete(Guid id) {
            var result = await _treatmentRoomService.DeleteAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除治疗室单成功");
        }

        [HttpGet("status/{status}")]
/// <summary>
/// 执行GetByStatus操作。
/// </summary>
/// <param name="status">参数status</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<TreatmentRoomDto>>> GetByStatus(string status) {
            var list = await _treatmentRoomService.GetByStatusAsync(status);
            return Ok(list);
        }
    }
}
