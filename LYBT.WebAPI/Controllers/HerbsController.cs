using LYBT.Module.Herbs.Dtos;
using LYBT.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using LYBT.Module.Herbs.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using LYBT.Common.Models;

namespace LYBT.Module.Herbs.Controllers {

    /// <summary>
    /// 药材管理 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/herbs")]
    [Authorize]
/// <summary>
/// 表示HerbsController。
/// </summary>
public class HerbsController : ControllerBase {
        private readonly IHerbService _herbService;

        /// <summary>
        /// 构造方法，注入药材服务
        /// </summary>
        public HerbsController(IHerbService herbService) {
            _herbService = herbService;
        }

        /// <summary>
        /// 获取药材列表
        /// </summary>
        [HttpGet]
/// <summary>
/// 执行GetList操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<HerbDto>>> GetList() {
            var list = await _herbService.GetListAsync();
            return Ok(list);
        }

        [HttpPost("paged")]
/// <summary>
/// 执行GetPaged操作。
/// </summary>
/// <param name="query">参数query</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<PagedResultDto<HerbDto>>> GetPaged([FromBody] HerbPagedQueryDto query) {
            var result = await _herbService.GetPagedAsync(query);
            return Ok(result);
        }

        /// <summary>
        /// 获取药材详情
        /// </summary>
        [HttpGet("{id}")]
/// <summary>
/// 执行GetById操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
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
/// <summary>
/// 执行Add操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
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
/// <summary>
/// 执行Update操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
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
/// <summary>
/// 执行Delete操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Delete(Guid id) {
            var result = await _herbService.DeleteAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除药材成功");
        }

        [HttpPost("import")]
/// <summary>
/// 执行Import操作。
/// </summary>
/// <param name="dtos">参数dtos</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Import([FromBody] List<HerbImportDto> dtos) {
            var count = await _herbService.ImportAsync(dtos);
            return Ok(new { Imported = count });
        }

        [HttpPost("export")]
/// <summary>
/// 执行Export操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<HerbDetailDto>>> Export() {
            var data = await _herbService.ExportAsync();
            return Ok(data);
        }

    }
}
