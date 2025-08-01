using Asp.Versioning;
using LYBT.Common.Models;
using LYBT.Models.FormulaTemplates;
using LYBT.Module.FormulaTemplates.Interfaces;
using LYBT.Shared.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 经验方模板 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
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
        public async Task<ActionResult<ApiResponse<bool>>> Add([FromBody] FormulaTemplateCreateDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<bool>.Fail("参数验证失败", 400));
            var result = await _service.AddAsync(dto);
            if (result)
                return Ok(ApiResponse<bool>.Success(result, "新增模板成功"));
            return BadRequest(ApiResponse<bool>.Fail("新增模板失败", 400));
        }

        /// <summary>
        /// 编辑模板
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<ApiResponse<bool>>> Update([FromBody] FormulaTemplateEditDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<bool>.Fail("参数验证失败", 400));
            var result = await _service.UpdateAsync(dto);
            if (result)
                return Ok(ApiResponse<bool>.Success(result, "编辑模板成功"));
            return BadRequest(ApiResponse<bool>.Fail("编辑模板失败", 400));
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id) {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(ApiResponse<bool>.Fail("删除模板失败", 404));
            return Ok(ApiResponse<bool>.Success(result, "删除模板成功"));
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
    }
}