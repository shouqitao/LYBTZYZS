using LYBT.Module.Prescriptions.Dtos;
using LYBT.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using LYBT.Module.Prescriptions.Services;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers {
    /// <summary>
    /// 处方管理 API
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
/// <summary>
/// 表示PrescriptionsController。
/// </summary>
public class PrescriptionsController : ControllerBase {
        private readonly IPrescriptionService _service;
        public PrescriptionsController(IPrescriptionService service) {
            _service = service;
        }

        [HttpGet]
/// <summary>
/// 执行GetList操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<PrescriptionDto>>> GetList() {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
/// <summary>
/// 执行GetById操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<PrescriptionDetailDto>> GetById(string id) {
            var detail = await _service.GetByIdAsync(id);
            if (detail == null)
                return NotFound();
            return Ok(detail);
        }

        [HttpPost]
/// <summary>
/// 执行Add操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Add([FromBody] PrescriptionCreateDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _service.CreateAsync(dto, Guid.Empty, "system");
            return result ? Ok() : BadRequest();
        }

        [HttpPut]
/// <summary>
/// 执行Update操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Update([FromBody] PrescriptionEditDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _service.UpdateAsync(dto, Guid.Empty, "system");
            return result ? Ok() : BadRequest();
        }

        [HttpDelete("{id}")]
/// <summary>
/// 执行Delete操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Delete(string id) {
            var result = await _service.DeleteAsync(id, Guid.Empty, "system");
            return result ? Ok() : NotFound();
        }

        [HttpPost("void/{id}")]
/// <summary>
/// 执行Cancel操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Cancel(string id) {
            var result = await _service.CancelAsync(id, Guid.Empty, "system");
            return result ? Ok() : NotFound();
        }
    }
}
