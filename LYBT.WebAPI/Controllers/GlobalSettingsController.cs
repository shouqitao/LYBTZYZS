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
            if (!result)
                return BadRequest();
            return Ok();
        }
    }
}