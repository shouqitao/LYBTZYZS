using LYBT.Module.FormulaTemplates.Dtos;
using LYBT.Module.FormulaTemplates.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace LYBT.Module.FormulaTemplates.Controllers {

    /// <summary>
    /// 经验方模板 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FormulaTemplateController : ControllerBase {
        private readonly IFormulaTemplateService _service;

        /// <summary>
        /// 构造方法，注入经验方模板服务
        /// </summary>
        public FormulaTemplateController(IFormulaTemplateService service) {
            _service = service;
        }

        /// <summary>
        /// 获取所有模板列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<FormulaTemplateDto>>> GetList() {
            var list = await _service.GetListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取模板详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<FormulaTemplateDetailDto>> GetById(Guid id) {
            var detail = await _service.GetByIdAsync(id);
            if (detail == null)
                return NotFound();
            return Ok(detail);
        }

        /// <summary>
        /// 新增模板
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Add([FromBody] FormulaTemplateCreateDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _service.AddAsync(dto);
            if (!result)
                return BadRequest("新增模板失败");
            return Ok("新增模板成功");
        }

        /// <summary>
        /// 编辑模板
        /// </summary>
        [HttpPut]
        public async Task<ActionResult> Update([FromBody] FormulaTemplateEditDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _service.UpdateAsync(dto);
            if (!result)
                return BadRequest("编辑模板失败");
            return Ok("编辑模板成功");
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id) {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除模板成功");
        }

        [HttpPost("import")]
        public async Task<ActionResult<object>> Import([FromBody] List<FormulaTemplateImportDto> dtos) {
            var count = await _service.ImportAsync(dtos);
            return Ok(new { Imported = count });
        }

        [HttpPost("export")]
        public async Task<ActionResult<List<FormulaTemplateDetailDto>>> Export() {
            var data = await _service.ExportAsync();
            return Ok(data);
        }

        [HttpPost("importExcel")]
        public async Task<ActionResult> ImportExcel(IFormFile file) {
            if (file == null || file.Length == 0)
                return BadRequest();
            var count = await _service.ImportFromExcelAsync(file.OpenReadStream());
            return Ok(new { Imported = count });
        }

        [HttpGet("exportExcel")]
        public async Task<FileContentResult> ExportExcel() {
            var bytes = await _service.ExportToExcelAsync();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "经典方.xlsx");
        }
    }
}