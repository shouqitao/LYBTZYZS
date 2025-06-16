using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Dtos;

namespace LYBT.Module.Herbs.Controllers {
    /// <summary>
    /// 药材管理 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HerbController : ControllerBase {
        private readonly IHerbService _herbService;

        /// <summary>
        /// 构造方法，注入药材服务
        /// </summary>
        public HerbController(IHerbService herbService) {
            _herbService = herbService;
        }

        /// <summary>
        /// 获取药材列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<HerbDto>>> GetList() {
            var list = await _herbService.GetListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取药材详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<HerbDetailDto>> GetById(Guid id) {
            var detail = await _herbService.GetByIdAsync(id);
            if (detail == null)
                return NotFound();
            return Ok(detail);
        }

        /// <summary>
        /// 新增药材
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Add([FromBody] HerbCreateDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _herbService.AddAsync(dto);
            if (!result)
                return BadRequest("新增药材失败");
            return Ok("新增药材成功");
        }

        /// <summary>
        /// 编辑药材
        /// </summary>
        [HttpPut]
        public async Task<ActionResult> Update([FromBody] HerbEditDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _herbService.UpdateAsync(dto);
            if (!result)
                return BadRequest("编辑药材失败");
            return Ok("编辑药材成功");
        }

        /// <summary>
        /// 删除药材
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id) {
            var result = await _herbService.DeleteAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除药材成功");
        }
    }
}
