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
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

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
                        .Include(m => m.Consultation)
                        .Include(m => m.Prescription)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (mc == null) return NotFound();
            return mc;
        }

        // POST /api/medicalcases
        [HttpPost]
        public async Task<IActionResult> CreateMedicalCase([FromBody] MedicalCase mc)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _db.MedicalCases.Add(mc);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMedicalCase), new { id = mc.Id }, mc);
        }

        // PUT /api/medicalcases/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMedicalCase(Guid id, [FromBody] MedicalCase updated)
        {
            if (id != updated.Id) return BadRequest("ID mismatch between URL and payload.");
            if (!ModelState.IsValid) return BadRequest(ModelState);
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

        // GET /api/medicalcases/search?patientName=&diagnosisKeyword=&startDate=&endDate=&page=&pageSize=
        [HttpGet("search")]
        public async Task<ActionResult<PagedResult<MedicalCase>>> Search(
            [FromQuery] string? patientName,
            [FromQuery] string? diagnosisKeyword,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var q = _db.MedicalCases
                .Include(m => m.Consultation)
                .Include(m => m.Prescription)
                .AsNoTracking()
                .Where(m => !m.IsDeleted);

            if (!string.IsNullOrWhiteSpace(patientName))
                q = q.Where(m => m.PatientName.Contains(patientName));

            if (!string.IsNullOrWhiteSpace(diagnosisKeyword))
                q = q.Where(m => m.Consultation != null && m.Consultation.TcmDiagnosis != null &&
                                 m.Consultation.TcmDiagnosis.Contains(diagnosisKeyword));

            if (startDate.HasValue)
                q = q.Where(m => m.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                q = q.Where(m => m.CreatedAt <= endDate.Value);

            var totalCount = await q.CountAsync();
            var items = await q
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<MedicalCase>(items, totalCount, page, pageSize);
        }

        // GET /api/medicalcases/query
        [HttpGet("query")]
        public async Task<ActionResult<PagedResult<MedicalCase>>> Query([FromBody] MedicalCaseQueryDto query)
        {
            var q = _db.MedicalCases
                .Include(m => m.Consultation)
                .Include(m => m.Prescription)
                .AsNoTracking()
                .Where(m => !m.IsDeleted);

            switch (query.QueryType)
            {
                case MedicalCaseQueryType.ByPatient:
                    if (query.PatientId.HasValue)
                        q = q.Where(m => m.PatientId == query.PatientId.Value);
                    break;

                case MedicalCaseQueryType.Pending:
                    if (query.DoctorId.HasValue)
                        q = q.Where(m => m.UserId == query.DoctorId.Value);
                    q = q.Where(m => m.CaseStatus == MedicalCaseStatus.Active && m.Prescription == null);
                    break;

                case MedicalCaseQueryType.Unfinished:
                    if (query.PatientId.HasValue)
                        q = q.Where(m => m.PatientId == query.PatientId.Value);
                    q = q.Where(m => m.CaseStatus != MedicalCaseStatus.Completed);
                    break;

                case MedicalCaseQueryType.Recent:
                    if (query.PatientId.HasValue)
                        q = q.Where(m => m.PatientId == query.PatientId.Value);
                    var limit = query.Limit ?? 10;
                    var recentItems = await q.OrderByDescending(m => m.CreatedAt).Take(limit).ToListAsync();
                    return new PagedResult<MedicalCase>(recentItems, recentItems.Count, 1, limit);

                case MedicalCaseQueryType.All:
                default:
                    break;
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
                q = q.Where(m => m.PatientName.Contains(query.Keyword) || (m.CaseNumber != null && m.CaseNumber.Contains(query.Keyword)));

            var totalCount = await q.CountAsync();
            var items = await q
                .OrderByDescending(m => m.CreatedAt)
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<MedicalCase>(items, totalCount, query.PageIndex, query.PageSize);
        }

        // POST /api/medicalcases/batch-details
        [HttpPost("batch-details")]
        public async Task<ActionResult<List<MedicalCase>>> GetBatchDetails([FromBody] List<Guid> ids)
        {
            if (ids == null || ids.Count > 50)
                return BadRequest("Batch size must not exceed 50 items.");

            var items = await _db.MedicalCases
                .Include(m => m.Consultation)
                .Include(m => m.Prescription)
                .AsNoTracking()
                .Where(m => ids.Contains(m.Id) && !m.IsDeleted)
                .ToListAsync();

            return items;
        }

        // GET /api/medicalcases/{id}/permissions
        [HttpGet("{id}/permissions")]
        public async Task<IActionResult> GetPermissions(Guid id)
        {
            var exists = await _db.MedicalCases.AsNoTracking().AnyAsync(m => m.Id == id && !m.IsDeleted);
            if (!exists) return NotFound();

            return Ok(new
            {
                CanEdit = true,
                CanDelete = true,
                RequiresEditReason = false,
                IsReadOnly = false,
                DenialReason = (string?)null
            });
        }

        // GET /api/medicalcases/by-status/{status}
        [HttpGet("by-status/{status}")]
        public async Task<ActionResult<List<MedicalCase>>> GetByStatus(MedicalCaseStatus status)
        {
            var items = await _db.MedicalCases
                .Include(m => m.Consultation)
                .Include(m => m.Prescription)
                .AsNoTracking()
                .Where(m => m.CaseStatus == status && !m.IsDeleted)
                .ToListAsync();

            return items;
        }

        // PUT /api/medicalcases/{id}/close
        [HttpPut("{id}/close")]
        public async Task<IActionResult> CloseCase(Guid id)
        {
            var mc = await _db.MedicalCases
                        .Include(m => m.Consultation)
                        .Include(m => m.Prescription)
                        .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (mc == null) return NotFound();
            if (mc.IsCompleted) return BadRequest("Medical case is already completed.");
            mc.Complete();
            await _db.SaveChangesAsync();
            return Ok(mc);
        }

        // PUT /api/medicalcases/{id}/suspend
        [HttpPut("{id}/suspend")]
        public async Task<IActionResult> SuspendCase(Guid id)
        {
            var mc = await _db.MedicalCases
                        .Include(m => m.Consultation)
                        .Include(m => m.Prescription)
                        .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (mc == null) return NotFound();
            if (mc.IsCompleted) return BadRequest("Medical case is already completed.");
            mc.Suspend();
            await _db.SaveChangesAsync();
            return Ok(mc);
        }

        // PUT /api/medicalcases/{id}/cancel
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelCase(Guid id)
        {
            var mc = await _db.MedicalCases.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (mc == null) return NotFound();
            var reg = await _db.Registrations.FirstOrDefaultAsync(r => r.MedicalCaseId == id);
            if (reg != null)
            {
                reg.Status = RegistrationStatus.Waiting;
            }
            mc.SoftDelete();
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // PUT /api/medicalcases/{id}/prescription-flag
        [HttpPut("{id}/prescription-flag")]
        public async Task<IActionResult> SetPrescriptionFlag(Guid id, [FromBody] SetPrescriptionFlagRequest request)
        {
            var mc = await _db.MedicalCases.FindAsync(id);
            if (mc == null || mc.IsDeleted) return NotFound();
            mc.NeedsPrescription = request.NeedsPrescription;
            mc.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(mc);
        }

        // PUT /api/medicalcases/{id}/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] MedicalCaseStatusInputDto request)
        {
            var mc = await _db.MedicalCases.FindAsync(id);
            if (mc == null || mc.IsDeleted) return NotFound();
            mc.CaseStatus = request.Status;
            mc.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(mc);
        }

        // PUT /api/medicalcases/{id}/print-completed
        [HttpPut("{id}/print-completed")]
        public async Task<IActionResult> RecordPrintCompleted(Guid id, [FromBody] PrintCompletedRequest request)
        {
            var mc = await _db.MedicalCases.FindAsync(id);
            if (mc == null || mc.IsDeleted) return NotFound();
            mc.PrintCount++;
            mc.IsPrinted = true;
            mc.LastPrintedAt = DateTime.UtcNow;
            mc.PrintVersion++;
            mc.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(mc);
        }

        // PUT /api/medicalcases/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> SaveAsync(Guid id, [FromBody] MedicalCaseInputDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _db.MedicalCases.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            existing.PatientId = request.PatientId;
            existing.UserId = request.UserId;
            existing.Remark = request.Remark;
            if (request.NeedsPrescription.HasValue)
                existing.NeedsPrescription = request.NeedsPrescription;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        // POST /api/medicalcases/batch-delete
        [HttpPost("batch-delete")]
        public async Task<IActionResult> BatchDelete([FromBody] List<Guid> ids)
        {
            if (ids == null || ids.Count == 0) return BadRequest("ID list cannot be empty.");
            var items = await _db.MedicalCases
                .Where(m => ids.Contains(m.Id) && !m.IsDeleted)
                .ToListAsync();
            foreach (var item in items)
            {
                item.IsDeleted = true;
                item.UpdatedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
            return Ok(new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                SuccessCount = items.Count
            });
        }
    }
}
