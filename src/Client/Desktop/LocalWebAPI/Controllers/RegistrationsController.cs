using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.Entities.Registrations;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;

using LYBT.LocalWebAPI.Mappers;

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
        public async Task<ActionResult<List<RegistrationListDto>>> GetRegistrations([FromQuery] DateTime? date)
        {
            var q = _db.Registrations.AsNoTracking().Where(r => !r.IsDeleted);
            if (date.HasValue)
            {
                q = q.Where(r => r.CreatedAt.Date == date.Value.Date);
            }
            return Ok((await q.ToListAsync()).Select(x => x.ToListDto()).ToList());
        }

        // GET /api/registrations/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<RegistrationDetailDto>> GetRegistration(Guid id)
        {
            var reg = await _db.Registrations.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
            if (reg == null) return NotFound();
            return Ok(reg.ToDetailDto());
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

        // GET /api/registrations/queue
        [HttpGet("queue")]
        public async Task<IActionResult> GetQueue([FromQuery] Guid? doctorId = null)
        {
            var q = _db.Registrations.AsNoTracking()
                .Where(r => !r.IsDeleted && r.Status == RegistrationStatus.Waiting);
            if (doctorId.HasValue)
            {
                q = q.Where(r => r.DoctorId == doctorId.Value);
            }
            var list = await q.OrderBy(r => r.CreatedAt).ToListAsync();
            return Ok(list);
        }

        // PUT /api/registrations/{id}/start-visit
        [HttpPut("{id}/start-visit")]
        public async Task<IActionResult> StartVisit(Guid id)
        {
            var reg = await _db.Registrations.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
            if (reg == null) return NotFound();
            if (reg.Status != RegistrationStatus.Waiting)
                return BadRequest("只有等待中的挂号才能接诊。");

            reg.Status = RegistrationStatus.InProgress;
            await _db.SaveChangesAsync();
            return Ok(reg.Id);
        }

        // PUT /api/registrations/{id}/cancel
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var reg = await _db.Registrations.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
            if (reg == null) return NotFound();
            if (reg.Status == RegistrationStatus.Completed)
                return BadRequest("已完成的挂号不能取消。");

            reg.Status = RegistrationStatus.Cancelled;
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
