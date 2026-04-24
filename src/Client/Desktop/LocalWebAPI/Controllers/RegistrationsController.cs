using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.Entities.Registrations;

namespace LYBT.LocalWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RegistrationsController : ControllerBase
    {
        private readonly LocalWebApiDbContext _db;

        public RegistrationsController(LocalWebApiDbContext db)
        {
            _db = db;
        }

        // GET /api/registrations?date=
        [HttpGet]
        public async Task<ActionResult<List<Registration>>> GetRegistrations([FromQuery] DateTime? date)
        {
            var q = _db.Registrations.AsNoTracking().Where(r => !r.IsDeleted);
            if (date.HasValue)
            {
                q = q.Where(r => r.Date.Date == date.Value.Date);
            }
            return await q.ToListAsync();
        }

        // GET /api/registrations/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Registration>> GetRegistration(Guid id)
        {
            var reg = await _db.Registrations.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
            if (reg == null) return NotFound();
            return reg;
        }

        // POST /api/registrations
        [HttpPost]
        public async Task<IActionResult> CreateRegistration([FromBody] Registration reg)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _db.Registrations.Add(reg);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetRegistration), new { id = reg.Id }, reg);
        }

        // PUT /api/registrations/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRegistration(Guid id, [FromBody] Registration updated)
        {
            if (id != updated.Id) return BadRequest("ID mismatch between URL and payload.");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var existing = await _db.Registrations.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            _db.Entry(existing).CurrentValues.SetValues(updated);
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        // DELETE /api/registrations/{id} -> soft delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRegistration(Guid id)
        {
            var existing = await _db.Registrations.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            existing.IsDeleted = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
