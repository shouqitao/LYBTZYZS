using LYBT.Module.TreatmentRoom.Dtos;
using LYBT.Module.TreatmentRoom.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Module.TreatmentRoom.Controllers {
    /// <summary>
    /// 治疗室 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
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
        public async Task<ActionResult<List<TreatmentRoomDto>>> GetList() {
            var list = await _treatmentRoomService.GetListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取治疗室单详情
        /// </summary>
        [HttpGet("{id}")]
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
        public async Task<ActionResult> Delete(Guid id) {
            var result = await _treatmentRoomService.DeleteAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除治疗室单成功");
        }
    }
}
