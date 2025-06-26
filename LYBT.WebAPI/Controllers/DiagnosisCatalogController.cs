using LYBT.Module.Settings.Dtos;
using LYBT.Module.Settings.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosisCatalogController : ControllerBase {
        private readonly IDiagnosisCatalogService _service;

        public DiagnosisCatalogController(IDiagnosisCatalogService service) {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<DiagnosisCatalogDto>>> GetAll() {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        [HttpPost]
        public async Task<ActionResult> Add([FromBody] DiagnosisCatalogCreateDto dto) {
            var result = await _service.AddAsync(dto);
            if (!result)
                return BadRequest();
            return Ok();
        }

        [HttpPut]
        public async Task<ActionResult> Update([FromBody] DiagnosisCatalogEditDto dto) {
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