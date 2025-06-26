using LYBT.Module.Settings.Dtos;
using LYBT.Module.Settings.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.Module.Settings.Controllers {

    /// <summary>
    /// 系统设置 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase {
        private readonly ISettingsService _settingsService;

        /// <summary>
        /// 构造方法，注入设置服务
        /// </summary>
        public SettingsController(ISettingsService settingsService) {
            _settingsService = settingsService;
        }

        /// <summary>
        /// 获取设置项列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<SettingsDto>>> GetList() {
            var list = await _settingsService.GetListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取设置项详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SettingsDetailDto>> GetById(Guid id) {
            var detail = await _settingsService.GetByIdAsync(id);
            if (detail == null)
                return NotFound();
            return Ok(detail);
        }

        /// <summary>
        /// 新增设置项
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Add([FromBody] SettingsCreateDto settingsCreateDto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _settingsService.AddAsync(settingsCreateDto);
            if (!result)
                return BadRequest("新增设置项失败");

            return Ok("新增设置项成功");
        }

        /// <summary>
        /// 编辑设置项
        /// </summary>
        [HttpPut]
        public async Task<ActionResult> Update([FromBody] SettingsEditDto settingsEditDto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _settingsService.UpdateAsync(settingsEditDto);
            if (!result)
                return BadRequest("编辑设置项失败");

            return Ok("编辑设置项成功");
        }

        /// <summary>
        /// 删除设置项
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id) {
            var result = await _settingsService.DeleteAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除设置项成功");
        }
    }
}