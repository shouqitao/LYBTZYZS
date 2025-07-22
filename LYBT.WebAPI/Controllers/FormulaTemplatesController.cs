using LYBT.Module.FormulaTemplates.Dtos;
using LYBT.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using LYBT.Module.FormulaTemplates.Interfaces;
using Microsoft.AspNetCore.Mvc;
using LYBT.Common.Models;
using Microsoft.AspNetCore.Http;

namespace LYBT.Module.FormulaTemplates.Controllers {

    /// <summary>
    /// 经验方模板 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
/// <summary>
/// 表示FormulaTemplateController。
/// </summary>
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
/// <summary>
/// 执行GetList操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<FormulaTemplateDto>>> GetList() {
            var list = await _service.GetListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取模板详情
        /// </summary>
        [HttpGet("{id}")]
/// <summary>
/// 执行GetById操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
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
/// <summary>
/// 执行Add操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<ApiSuccessResponse>> Add([FromBody] FormulaTemplateCreateDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(new ApiSuccessResponse { Success = false, Message = "参数验证失败" });
            var result = await _service.AddAsync(dto);
            if (result)
                return Ok(new ApiSuccessResponse { Success = true, Message = "新增模板成功" });
            return BadRequest(new ApiSuccessResponse { Success = false, Message = "新增模板失败" });
        }

        /// <summary>
        /// 编辑模板
        /// </summary>
        [HttpPut]
/// <summary>
/// 执行Update操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<ApiSuccessResponse>> Update([FromBody] FormulaTemplateEditDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(new ApiSuccessResponse { Success = false, Message = "参数验证失败" });
            var result = await _service.UpdateAsync(dto);
            if (result)
                return Ok(new ApiSuccessResponse { Success = true, Message = "编辑模板成功" });
            return BadRequest(new ApiSuccessResponse { Success = false, Message = "编辑模板失败" });
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        [HttpDelete("{id}")]
/// <summary>
/// 执行Delete操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<ApiSuccessResponse>> Delete(Guid id) {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new ApiSuccessResponse { Success = false, Message = "删除模板失败" });
            return Ok(new ApiSuccessResponse { Success = true, Message = "删除模板成功" });
        }

        [HttpPost("import")]
/// <summary>
/// 执行Import操作。
/// </summary>
/// <param name="dtos">参数dtos</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<object>> Import([FromBody] List<FormulaTemplateImportDto> dtos) {
            var count = await _service.ImportAsync(dtos);
            return Ok(new { Imported = count });
        }

        [HttpPost("export")]
/// <summary>
/// 执行Export操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<FormulaTemplateDetailDto>>> Export() {
            var data = await _service.ExportAsync();
            return Ok(data);
        }

    }
}
