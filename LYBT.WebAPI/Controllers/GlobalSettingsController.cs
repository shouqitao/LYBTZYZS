using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using LYBT.Module.Settings.Interfaces;
using LYBT.Module.Settings.Dtos;

namespace LYBT.WebAPI.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class GlobalSettingsController : ControllerBase {
        private readonly IGlobalSettingsService _service;
        public GlobalSettingsController(IGlobalSettingsService service) {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<GlobalSettingsDto?>> GetSettings() {
            var settings = await _service.GetAsync();
            return Ok(settings);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateSettings([FromBody] GlobalSettingsDto dto) {
            var result = await _service.SaveAsync(dto);
            if (!result) return BadRequest();
            return Ok();
        }
    }
}
