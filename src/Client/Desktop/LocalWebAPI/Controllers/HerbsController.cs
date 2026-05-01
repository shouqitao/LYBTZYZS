using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.Entities.Herbs;

namespace LYBT.LocalWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HerbsController : ControllerBase
    {
        private readonly LocalWebApiDbContext _db;

        public HerbsController(LocalWebApiDbContext db)
        {
            _db = db;
        }

        // GET /api/herbs?keyword=
        [HttpGet]
        public async Task<ActionResult<List<Herb>>> GetHerbs([FromQuery] string keyword)
        {
            var q = _db.Herbs.AsNoTracking().Where(h => !h.IsDeleted);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                q = q.Where(h => h.Name.Contains(keyword) || (h.PinYinCode != null && h.PinYinCode.Contains(keyword)));
            }
            return await q.ToListAsync();
        }

        // GET /api/herbs/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Herb>> GetHerb(Guid id)
        {
            var herb = await _db.Herbs.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted);
            if (herb == null) return NotFound();
            return herb;
        }

        // POST /api/herbs
        [HttpPost]
        public async Task<IActionResult> CreateHerb([FromBody] Herb herb)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _db.Herbs.Add(herb);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetHerb), new { id = herb.Id }, herb);
        }

        // PUT /api/herbs/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHerb(Guid id, [FromBody] Herb updated)
        {
            if (id != updated.Id) return BadRequest("ID mismatch between URL and payload.");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var existing = await _db.Herbs.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            _db.Entry(existing).CurrentValues.SetValues(updated);
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        // DELETE /api/herbs/{id} -> soft delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHerb(Guid id)
        {
            var existing = await _db.Herbs.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            existing.IsDeleted = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
