using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.Entities.Herbs;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;

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

        // POST /api/herbs/batch-delete
        [HttpPost("batch-delete")]
        public async Task<IActionResult> BatchDelete([FromBody] List<Guid> ids)
        {
            if (ids == null || ids.Count == 0) return BadRequest("ids 不能为空");

            var isReferenced = await _db.PrescriptionItems.AnyAsync(pi => ids.Contains(pi.HerbId));
            if (isReferenced)
                return Conflict("部分药材被处方引用，无法删除");

            var herbs = await _db.Herbs.Where(h => ids.Contains(h.Id) && !h.IsDeleted).ToListAsync();
            foreach (var h in herbs) h.IsDeleted = true;
            await _db.SaveChangesAsync();
            return Ok(new { count = herbs.Count });
        }

        // POST /api/herbs/batch-enable
        [HttpPost("batch-enable")]
        public async Task<IActionResult> BatchEnable([FromBody] List<Guid> ids)
        {
            if (ids == null || ids.Count == 0) return BadRequest("ids 不能为空");

            var herbs = await _db.Herbs.Where(h => ids.Contains(h.Id) && !h.IsDeleted).ToListAsync();
            foreach (var h in herbs) h.Status = CommonStatus.Enabled;
            await _db.SaveChangesAsync();
            return Ok(new { count = herbs.Count });
        }

        // POST /api/herbs/batch-disable
        [HttpPost("batch-disable")]
        public async Task<IActionResult> BatchDisable([FromBody] List<Guid> ids)
        {
            if (ids == null || ids.Count == 0) return BadRequest("ids 不能为空");

            var herbs = await _db.Herbs.Where(h => ids.Contains(h.Id) && !h.IsDeleted).ToListAsync();
            foreach (var h in herbs) h.Status = CommonStatus.Disabled;
            await _db.SaveChangesAsync();
            return Ok(new { count = herbs.Count });
        }

        // POST /api/herbs/{id}/toggle-status
        [HttpPost("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var herb = await _db.Herbs.FindAsync(id);
            if (herb == null || herb.IsDeleted) return NotFound();
            herb.Status = herb.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled;
            await _db.SaveChangesAsync();
            return Ok(herb);
        }

        // POST /api/herbs/{id}/restore
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> Restore(Guid id)
        {
            var herb = await _db.Herbs.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == id);
            if (herb == null) return NotFound();
            herb.IsDeleted = false;
            await _db.SaveChangesAsync();
            return Ok(herb);
        }

        // GET /api/herbs/export
        [HttpGet("export")]
        public async Task<ActionResult<List<Herb>>> Export()
        {
            return await _db.Herbs.AsNoTracking().Where(h => !h.IsDeleted).ToListAsync();
        }

        // POST /api/herbs/import
        [HttpPost("import")]
        public async Task<IActionResult> Import([FromBody] List<Herb> herbs)
        {
            if (herbs == null || herbs.Count == 0) return BadRequest("导入列表不能为空");

            var ids = herbs.Select(h => h.Id).Where(id => id != Guid.Empty).ToList();
            var existingIds = ids.Count > 0
                ? (await _db.Herbs.IgnoreQueryFilters().Where(h => ids.Contains(h.Id)).Select(h => h.Id).ToListAsync()).ToHashSet()
                : new HashSet<Guid>();

            int count = 0;
            foreach (var herb in herbs)
            {
                if (string.IsNullOrWhiteSpace(herb.PinYinCode) && !string.IsNullOrWhiteSpace(herb.Name))
                    herb.PinYinCode = herb.Name; // client-side pinyin generation not available server-side; use name as fallback

                if (herb.Id != Guid.Empty && existingIds.Contains(herb.Id))
                {
                    var existing = await _db.Herbs.IgnoreQueryFilters().FirstAsync(h => h.Id == herb.Id);
                    _db.Entry(existing).CurrentValues.SetValues(herb);
                }
                else
                {
                    if (herb.Id == Guid.Empty) herb.Id = Guid.NewGuid();
                    _db.Herbs.Add(herb);
                }
                count++;
            }

            await _db.SaveChangesAsync();
            return Ok(new { count });
        }

        // GET /api/herbs/categories
        [HttpGet("categories")]
        public async Task<ActionResult<List<string>>> GetCategories()
        {
            return await _db.Herbs.AsNoTracking()
                .Where(h => !h.IsDeleted && h.Category != null)
                .Select(h => h.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
    }
}
