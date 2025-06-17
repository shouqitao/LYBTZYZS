using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Settings.Interfaces;
using LYBT.Module.Settings.Dtos;

namespace LYBT.WebAPI.Controllers {
    [ApiController]
    [Route("api/[controller]")]
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
            if (!result) return BadRequest();
            return Ok();
        }

        [HttpPut]
        public async Task<ActionResult> Update([FromBody] TreatmentCatalogEditDto dto) {
            var result = await _service.UpdateAsync(dto);
            if (!result) return BadRequest();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id) {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return Ok();
        }
    }
}
