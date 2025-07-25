using LYBT.Module.Settings.Interfaces;
using LYBT.Module.Settings.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers {

    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class TreatmentCatalogController : ControllerBase {
        private readonly ITreatmentCatalogService _service;

        public TreatmentCatalogController(ITreatmentCatalogService service) {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<TreatmentCatalogDto>>> GetAll() {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        [HttpPost]
        public async Task<ActionResult> Add([FromBody] TreatmentCatalogCreateDto dto) {
            var result = await _service.AddAsync(dto);
            if (!result)
                return BadRequest();
            return Ok();
        }

        [HttpPut]
        public async Task<ActionResult> Update([FromBody] TreatmentCatalogEditDto dto) {
            var result = await _service.UpdateAsync(dto);
            if (!result)
                return BadRequest();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id) {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound();
            return Ok();
        }
    }
}