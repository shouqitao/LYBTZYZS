using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Consultations;
using LYBT.Entities.Prescriptions;

namespace LYBT.LocalWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MedicalCasesController : ControllerBase
    {
        private readonly LocalWebApiDbContext _db;

        public MedicalCasesController(LocalWebApiDbContext db)
        {
            _db = db;
        }

        // GET /api/medicalcases?patientId=
        [HttpGet]
        public async Task<ActionResult<List<MedicalCase>>> GetMedicalCases([FromQuery] Guid? patientId)
        {
            var q = _db.MedicalCases.AsNoTracking().Where(m => !m.IsDeleted);
            if (patientId.HasValue)
            {
                q = q.Where(m => m.PatientId == patientId.Value);
            }
            return await q.ToListAsync();
        }

        // GET /api/medicalcases/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MedicalCase>> GetMedicalCase(Guid id)
        {
            // Include navigation properties for detail as requested
            var mc = await _db.MedicalCases
                        .Include(m => m.Consultations)
                        .Include(m => m.Prescriptions)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (mc == null) return NotFound();
            return mc;
        }

        // POST /api/medicalcases
        [HttpPost]
        public async Task<IActionResult> CreateMedicalCase([FromBody] MedicalCase mc)
        {
            _db.MedicalCases.Add(mc);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMedicalCase), new { id = mc.Id }, mc);
        }

        // PUT /api/medicalcases/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMedicalCase(Guid id, [FromBody] MedicalCase updated)
        {
            var existing = await _db.MedicalCases.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            _db.Entry(existing).CurrentValues.SetValues(updated);
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        // DELETE /api/medicalcases/{id} -> soft delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMedicalCase(Guid id)
        {
            var existing = await _db.MedicalCases.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            existing.IsDeleted = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
