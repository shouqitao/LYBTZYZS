using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.Entities.Patients;
using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Enums;

namespace LYBT.LocalWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientsController : ControllerBase
    {
        private readonly LocalWebApiDbContext _db;

        public PatientsController(LocalWebApiDbContext db)
        {
            _db = db;
        }

        // GET /api/patients?keyword=&page=&pageSize=
        [HttpGet]
        public async Task<ActionResult<List<Patient>>> GetPatients([FromQuery] string keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            // Pagination bounds validation
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;
            var q = _db.Patients.AsNoTracking().Where(p => !p.IsDeleted);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                q = q.Where(p => p.Name.Contains(keyword) || (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)));
            }
            var list = await q
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return list;
        }

        // GET /api/patients/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Patient>> GetPatient(Guid id)
        {
            var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (patient == null) return NotFound();
            return patient;
        }

        // POST /api/patients
        [HttpPost]
        public async Task<IActionResult> CreatePatient([FromBody] Patient patient)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPatient), new { id = patient.Id }, patient);
        }

        // PUT /api/patients/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] Patient updated)
        {
            if (id != updated.Id) return BadRequest("ID mismatch between URL and payload.");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var existing = await _db.Patients.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            _db.Entry(existing).CurrentValues.SetValues(updated);
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        // DELETE /api/patients/{id} -> soft delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(Guid id)
        {
            var existing = await _db.Patients.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            existing.IsDeleted = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // GET /api/patients/by-id-number/{idNumber}
        [HttpGet("by-id-number/{idNumber}")]
        public async Task<ActionResult<Patient>> GetPatientByIdNumber(string idNumber)
        {
            var patient = await _db.Patients.AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.IdNumber == idNumber);
            if (patient == null) return NotFound();
            return patient;
        }

        // POST /api/patients/batch-delete
        [HttpPost("batch-delete")]
        public async Task<IActionResult> BatchDeletePatients([FromBody] List<Guid> ids)
        {
            if (ids == null || ids.Count == 0) return BadRequest("No IDs provided.");
            var hasActiveCases = await _db.MedicalCases
                .AnyAsync(mc => ids.Contains(mc.PatientId) && !mc.IsDeleted);
            if (hasActiveCases)
                return Conflict(new { message = "部分患者存在医案记录，无法删除" });
            var patients = await _db.Patients
                .Where(p => ids.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync();
            foreach (var p in patients)
                p.IsDeleted = true;
            await _db.SaveChangesAsync();
            return Ok(new { successCount = patients.Count, failureCount = ids.Count - patients.Count });
        }

        // POST /api/patients/{id}/restore
        [HttpPost("{id}/restore")]
        public async Task<ActionResult<Patient>> RestorePatient(Guid id)
        {
            var patient = await _db.Patients.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted);
            if (patient == null) return NotFound();
            patient.IsDeleted = false;
            await _db.SaveChangesAsync();
            return patient;
        }

        // POST /api/patients/{id}/toggle-status
        [HttpPost("{id}/toggle-status")]
        public async Task<ActionResult<Patient>> TogglePatientStatus(Guid id)
        {
            var patient = await _db.Patients.FindAsync(id);
            if (patient == null || patient.IsDeleted) return NotFound();
            patient.Status = patient.Status == CommonStatus.Enabled
                ? CommonStatus.Disabled
                : CommonStatus.Enabled;
            await _db.SaveChangesAsync();
            return patient;
        }

        // GET /api/patients/export
        [HttpGet("export")]
        public async Task<ActionResult<List<Patient>>> ExportPatients()
        {
            var patients = await _db.Patients.AsNoTracking()
                .Where(p => !p.IsDeleted)
                .ToListAsync();
            return patients;
        }

        // POST /api/patients/import
        [HttpPost("import")]
        public async Task<IActionResult> ImportPatients([FromBody] List<Patient> patients)
        {
            if (patients == null || patients.Count == 0) return BadRequest("No patients provided.");
            var existingIds = patients.Select(p => p.Id).ToList();
            var existing = await _db.Patients.IgnoreQueryFilters()
                .Where(p => existingIds.Contains(p.Id))
                .ToListAsync();
            var existingDict = existing.ToDictionary(p => p.Id);
            int count = 0;
            foreach (var patient in patients)
            {
                if (existingDict.TryGetValue(patient.Id, out var dbPatient))
                    _db.Entry(dbPatient).CurrentValues.SetValues(patient);
                else
                    _db.Patients.Add(patient);
                count++;
            }
            await _db.SaveChangesAsync();
            return Ok(new { importedCount = count });
        }
    }
}
