using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Enums;

using LYBT.LocalWebAPI.Mappers;

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
        public async Task<ActionResult<List<PatientListDto>>> GetPatients([FromQuery] string keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
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
            return Ok(list.Select(x => x.ToListDto()).ToList());
        }

        // GET /api/patients/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Patient>> GetPatient(Guid id)
        {
            var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (patient == null) return NotFound();
            return Ok(patient.ToDetailDto());
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
        public async Task<ActionResult<PatientDetailDto>> GetPatientByIdNumber(string idNumber)
        {
            var patient = await _db.Patients.AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.IdNumber == idNumber);
            if (patient == null) return NotFound();
            return Ok(patient.ToDetailDto());
        }

        // POST /api/patients/batch-delete
        [HttpPost("batch-delete")]
        public async Task<IActionResult> BatchDeletePatients([FromBody] BatchDeleteInputDto request)
        {
            if (request?.Ids == null || request.Ids.Count == 0) return BadRequest("No IDs provided.");
            var hasActiveCases = await _db.MedicalCases
                .AnyAsync(mc => request.Ids.Contains(mc.PatientId) && !mc.IsDeleted);
            if (hasActiveCases)
                return Conflict(new { message = "部分患者存在医案记录，无法删除" });
            var patients = await _db.Patients
                .Where(p => request.Ids.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync();
            foreach (var p in patients)
                p.IsDeleted = true;
            await _db.SaveChangesAsync();
            return Ok(new { successCount = patients.Count, failureCount = request.Ids.Count - patients.Count });
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
            return Ok(patient.ToDetailDto());
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
            return Ok(patient.ToDetailDto());
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

        // GET /api/patients/import-template
        [HttpGet("import-template")]
        public IActionResult ExportTemplate()
        {
            var template = new[]
            {
                new { Name = "", IdNumber = "", Gender = (int?)null, BirthDate = (DateTime?)null, PhoneNumber = "", Address = "" }
            };
            return Ok(template);
        }

        // POST /api/patients/import
        [HttpPost("import")]
        public async Task<ActionResult<PatientBatchImportResultDto>> ImportPatients([FromBody] PatientBatchImportInputDto request)
        {
            if (request == null || request.Patients == null || request.Patients.Count == 0)
                return BadRequest("患者列表不能为空");

            var result = new PatientBatchImportResultDto { ImportTime = DateTime.UtcNow, TotalCount = request.Patients.Count };

            foreach (var dto in request.Patients)
            {
                try
                {
                    var entity = new Patient
                    {
                        Id = Guid.NewGuid(),
                        Name = dto.Name,
                        PinYinCode = dto.PinYinCode,
                        Gender = dto.Gender,
                        BirthDate = dto.BirthDate,
                        IdNumber = dto.IdNumber,
                        PhoneNumber = dto.PhoneNumber,
                        Address = dto.Address,
                        MaritalStatus = dto.MaritalStatus,
                        IdType = dto.IdType,
                        BloodType = dto.BloodType,
                        AllergyHistory = dto.AllergyHistory,
                        MedicalHistory = dto.MedicalHistory,
                        Status = CommonStatus.Enabled
                    };

                    _db.Patients.Add(entity);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Failures.Add(new PatientImportFailureDto
                    {
                        FailureReason = ex.Message,
                        FieldName = "Unknown"
                    });
                }
            }

            await _db.SaveChangesAsync();
            return Ok(result);
        }

        // GET /api/patients/{id}/check-reference
        [HttpGet("{id}/check-reference")]
        public async Task<IActionResult> CheckReference(Guid id)
        {
            var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (patient == null) return NotFound();

            var hasActiveCases = await _db.MedicalCases.AnyAsync(mc => mc.PatientId == id && !mc.IsDeleted);

            return Ok(new
            {
                IsReferenced = hasActiveCases,
                ReferenceCount = hasActiveCases ? await _db.MedicalCases.CountAsync(mc => mc.PatientId == id && !mc.IsDeleted) : 0
            });
        }

        // POST /api/patients/batch-check-reference
        [HttpPost("batch-check-reference")]
        public async Task<IActionResult> BatchCheckReference([FromBody] BatchDeleteInputDto request)
        {
            if (request?.Ids == null || request.Ids.Count == 0)
                return BadRequest("ids 不能为空");

            var referencedPatients = await _db.MedicalCases
                .Where(mc => request.Ids.Contains(mc.PatientId) && !mc.IsDeleted)
                .GroupBy(mc => mc.PatientId)
                .Select(g => new { PatientId = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = request.Ids.Select(id => new
            {
                PatientId = id,
                IsReferenced = referencedPatients.Any(r => r.PatientId == id),
                ReferenceCount = referencedPatients.FirstOrDefault(r => r.PatientId == id)?.Count ?? 0
            }).ToList();

            return Ok(result);
        }
    }
}
