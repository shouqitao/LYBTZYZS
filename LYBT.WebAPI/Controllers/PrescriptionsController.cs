using LYBT.Module.Prescriptions.Dtos;
using LYBT.Module.Prescriptions.Services;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers {
    /// <summary>
    /// 处方管理 API
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PrescriptionsController : ControllerBase {
        private readonly IPrescriptionService _service;
        public PrescriptionsController(IPrescriptionService service) {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<PrescriptionDto>>> GetList() {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PrescriptionDetailDto>> GetById(string id) {
            var detail = await _service.GetByIdAsync(id);
            if (detail == null)
                return NotFound();
            return Ok(detail);
        }

        [HttpPost]
        public async Task<ActionResult> Add([FromBody] PrescriptionCreateDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _service.CreateAsync(dto, Guid.Empty, "system");
            return result ? Ok() : BadRequest();
        }

        [HttpPut]
        public async Task<ActionResult> Update([FromBody] PrescriptionEditDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _service.UpdateAsync(dto, Guid.Empty, "system");
            return result ? Ok() : BadRequest();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id) {
            var result = await _service.DeleteAsync(id, Guid.Empty, "system");
            return result ? Ok() : NotFound();
        }

        [HttpPost("void/{id}")]
        public async Task<ActionResult> Cancel(string id) {
            var result = await _service.CancelAsync(id, Guid.Empty, "system");
            return result ? Ok() : NotFound();
        }
    }
}
