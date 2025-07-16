using LYBT.Module.Herbs.Dtos;
using LYBT.Module.Herbs.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace LYBT.Module.Herbs.Controllers {

    /// <summary>
    /// 药材管理 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/herbs")]
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

        [HttpPost("import")]
        public async Task<ActionResult> Import([FromBody] List<HerbImportDto> dtos) {
            var count = await _herbService.ImportAsync(dtos);
            return Ok(new { Imported = count });
        }

        [HttpPost("export")]
        public async Task<ActionResult<List<HerbDetailDto>>> Export() {
            var data = await _herbService.ExportAsync();
            return Ok(data);
        }

        [HttpPost("importExcel")]
        public async Task<ActionResult> ImportExcel(IFormFile file) {
            if (file == null || file.Length == 0)
                return BadRequest();
            var count = await _herbService.ImportFromExcelAsync(file.OpenReadStream());
            return Ok(new { Imported = count });
        }

        [HttpGet("exportExcel")]
        public async Task<FileContentResult> ExportExcel() {
            var bytes = await _herbService.ExportToExcelAsync();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "药材.xlsx");
        }
    }
}