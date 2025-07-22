using LYBT.Module.Settings.Dtos;
using LYBT.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using LYBT.Module.Settings.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers {

    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
/// <summary>
/// 表示DiagnosisCatalogController。
/// </summary>
public class DiagnosisCatalogController : ControllerBase {
        private readonly IDiagnosisCatalogService _service;

        public DiagnosisCatalogController(IDiagnosisCatalogService service) {
            _service = service;
        }

        [HttpGet]
/// <summary>
/// 执行GetAll操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<DiagnosisCatalogDto>>> GetAll() {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        [HttpPost]
/// <summary>
/// 执行Add操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Add([FromBody] DiagnosisCatalogCreateDto dto) {
            var result = await _service.AddAsync(dto);
            if (!result)
                return BadRequest();
            return Ok();
        }

        [HttpPut]
/// <summary>
/// 执行Update操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Update([FromBody] DiagnosisCatalogEditDto dto) {
            var result = await _service.UpdateAsync(dto);
            if (!result)
                return BadRequest();
            return Ok();
        }

        [HttpDelete("{id}")]
/// <summary>
/// 执行Delete操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Delete(Guid id) {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound();
            return Ok();
        }
    }
}
