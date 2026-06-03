using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Entities.Consultations;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

using LYBT.LocalWebAPI.Mappers;

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

        // GET /api/medicalcases?status=&patientId=&page=&pageSize=&includeAllDoctors=&keyword=
        [HttpGet]
        public async Task<ActionResult<PagedResult<MedicalCaseListDto>>> GetMedicalCases(
            [FromQuery] MedicalCaseStatus? status = null,
            [FromQuery] Guid? patientId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeAllDoctors = false,
            [FromQuery] string? keyword = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var q = _db.MedicalCases
                .Include(m => m.Consultation)
                .Include(m => m.Prescription)
                .AsNoTracking()
                .Where(m => !m.IsDeleted);

            if (status.HasValue)
                q = q.Where(m => m.CaseStatus == status.Value);

            if (patientId.HasValue)
                q = q.Where(m => m.PatientId == patientId.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
                q = q.Where(m => m.PatientName.Contains(keyword) || (m.CaseNumber != null && m.CaseNumber.Contains(keyword)));

            var totalCount = await q.CountAsync();
            var items = await q
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = items.Select(x => x.ToListDto()).ToList();
            return new PagedResult<MedicalCaseListDto>(dtos, totalCount, page, pageSize);
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
        public async Task<ActionResult<PagedResult<MedicalCase>>> Query([FromQuery] MedicalCaseQueryDto query)
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
        public async Task<ActionResult<List<MedicalCase>>> GetBatchDetails([FromBody] BatchDetailQueryDto request)
        {
            if (request?.Ids == null || request.Ids.Count > 50)
                return BadRequest("Batch size must not exceed 50 items.");

            var items = await _db.MedicalCases
                .Include(m => m.Consultation)
                .Include(m => m.Prescription)
                .AsNoTracking()
                .Where(m => request.Ids.Contains(m.Id) && !m.IsDeleted)
                .ToListAsync();

            return items;
        }

        // GET /api/medicalcases/{id}/permissions
        [HttpGet("{id}/permissions")]
        public async Task<IActionResult> GetPermissions(Guid id)
        {
            var mc = await _db.MedicalCases
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (mc == null) return NotFound();

            // Extract current user from JWT
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("nameid")?.Value;
            var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value;

            var userId = Guid.TryParse(userIdClaim, out var uid) ? uid : Guid.Empty;
            var isAdmin = roleClaim == "Admin" || roleClaim == "SuperAdmin";
            var isOwner = mc.UserId == userId;

            // RBAC logic aligned with Server MedicalCasePermissionService
            bool canEdit;
            if (isAdmin)
            {
                canEdit = true;
            }
            else if (isOwner)
            {
                // Draft/Active: always editable
                if (mc.CaseStatus == MedicalCaseStatus.Active || mc.CaseStatus == MedicalCaseStatus.Suspended)
                    canEdit = true;
                // Completed: editable only today
                else if (mc.CaseStatus == MedicalCaseStatus.Completed)
                {
                    var completionDate = (mc.CompletedAt ?? mc.CreatedAt).Date;
                    canEdit = completionDate == DateTime.Today;
                }
                else
                    canEdit = false;
            }
            else
            {
                canEdit = false;
            }

            var canDelete = canEdit; // same rules
            var requiresEditReason = mc.CaseStatus == MedicalCaseStatus.Completed;

            return Ok(new MedicalCasePermissionDto
            {
                CanEdit = canEdit,
                CanDelete = canDelete,
                RequiresEditReason = requiresEditReason,
                DenialReason = canEdit ? null : (isOwner ? "该医案已完成且已过当天编辑时间" : "您不是该医案的创建者，无权编辑")
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
            return Ok(mc.ToDetailDto());
        }

        // PUT /api/medicalcases/{id}/suspend
        [HttpPut("{id}/suspend")]
        public async Task<IActionResult> SuspendCase(Guid id, [FromBody] ConsultationInputDto? request = null)
        {
            var mc = await _db.MedicalCases
                        .Include(m => m.Consultation)
                        .Include(m => m.Prescription)
                        .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (mc == null) return NotFound();
            if (mc.IsCompleted) return BadRequest("Medical case is already completed.");
            mc.Suspend();
            await _db.SaveChangesAsync();
            return Ok(mc.ToDetailDto());
        }

        // PUT /api/medicalcases/{id}/cancel
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelCase(Guid id, [FromBody] CancelMedicalCaseRequestDto? request = null)
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
            return Ok(mc.ToDetailDto());
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
            return Ok(mc.ToDetailDto());
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
            return Ok(mc.ToDetailDto());
        }

        // PUT /api/medicalcases/{id} - aggregate save (Consultation + Prescription)
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> SaveAsync(Guid id, [FromBody] MedicalCaseInputDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (request.Id != id) return BadRequest("请求ID与路由ID不一致");

            var mc = await _db.MedicalCases
                .Include(m => m.Consultation)
                .Include(m => m.Prescription)
                    .ThenInclude(p => p!.Items)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (mc == null) return NotFound();

            // Update MedicalCase fields
            mc.PatientId = request.PatientId;
            mc.UserId = request.UserId;
            mc.Remark = request.Remark;
            if (request.NeedsPrescription.HasValue)
                mc.NeedsPrescription = request.NeedsPrescription.Value;
            mc.UpdatedAt = DateTime.UtcNow;

            // Upsert Consultation
            if (request.Consultation != null)
            {
                if (mc.Consultation == null)
                {
                    mc.Consultation = new Consultation
                    {
                        Id = mc.Id
                    };
                    _db.Consultations.Add(mc.Consultation);
                }
                mc.Consultation.PresentIllness = request.Consultation.PresentIllness;
                mc.Consultation.TongueDiagnosis = request.Consultation.TongueDiagnosis;
                mc.Consultation.PulseDiagnosis = request.Consultation.PulseDiagnosis;
                mc.Consultation.TcmDiagnosis = request.Consultation.TcmDiagnosis;
                mc.Consultation.UpdatedAt = DateTime.UtcNow;
            }

            // Upsert or delete Prescription
            if (request.Prescription != null && request.Prescription.NeedsPrescription)
            {
                if (mc.Prescription == null)
                {
                    mc.Prescription = new Prescription
                    {
                        Id = Guid.NewGuid(),
                        MedicalCaseId = mc.Id
                    };
                    _db.Prescriptions.Add(mc.Prescription);
                }
                mc.Prescription.DosageCount = request.Prescription.DosageCount;
                mc.Prescription.Usage = request.Prescription.Usage;
                mc.Prescription.Advice = request.Prescription.Advice;
                mc.Prescription.ReferencedFormulas = request.Prescription.ReferencedFormulas;
                mc.Prescription.Discount = request.Prescription.Discount;
                mc.Prescription.Remark = request.Prescription.Remark;
                mc.Prescription.UpdatedAt = DateTime.UtcNow;

                // Replace prescription items
                if (mc.Prescription.Items?.Any() == true)
                    _db.PrescriptionItems.RemoveRange(mc.Prescription.Items);

                mc.Prescription.Items = request.Prescription.Items.Select(item => new PrescriptionItem
                {
                    Id = Guid.NewGuid(),
                    PrescriptionId = mc.Prescription.Id,
                    HerbId = item.HerbId,
                    HerbName = item.HerbName ?? string.Empty,
                    Dosage = item.Dosage,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    DecocteMethod = item.DecocteMethod,
                    Usage = item.Usage,
                    Remark = item.Remark
                }).ToList();

                mc.NeedsPrescription = true;
            }
            else if (request.Prescription != null && !request.Prescription.NeedsPrescription && mc.Prescription != null)
            {
                // Soft-delete prescription when NeedsPrescription=false
                if (mc.Prescription.Items?.Any() == true)
                    _db.PrescriptionItems.RemoveRange(mc.Prescription.Items);
                _db.Prescriptions.Remove(mc.Prescription);
                mc.Prescription = null;
                mc.NeedsPrescription = false;
            }

            await _db.SaveChangesAsync();
            return Ok(mc.ToDetailDto());
        }

        // POST /api/medicalcases/batch-delete
        [HttpPost("batch-delete")]
        public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto request)
        {
            if (request?.Ids == null || request.Ids.Count == 0) return BadRequest("ID list cannot be empty.");
            var items = await _db.MedicalCases
                .Where(m => request.Ids.Contains(m.Id) && !m.IsDeleted)
                .ToListAsync();
            foreach (var item in items)
            {
                item.IsDeleted = true;
                item.UpdatedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
            return Ok(new BatchOperationResultDto
            {
                TotalCount = request.Ids.Count,
                SuccessCount = items.Count
            });
        }

        // GET /api/medicalcases/pending
        [HttpGet("pending")]
        public async Task<ActionResult<List<PendingMedicalCaseDto>>> GetPendingCases([FromQuery] Guid? patientId = null)
        {
            var query = _db.MedicalCases
                .AsNoTracking()
                .Where(m => !m.IsDeleted && m.CaseStatus == MedicalCaseStatus.Active);

            if (patientId.HasValue)
                query = query.Where(m => m.PatientId == patientId.Value);

            var items = await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return items.Select(m => new PendingMedicalCaseDto
            {
                PatientId = m.PatientId,
                PatientName = m.PatientName,
                CaseStatus = m.CaseStatus,
                MedicalCaseId = m.Id,
                CreatedAt = m.CreatedAt
            }).ToList();
        }

        // GET /api/medicalcases/{id}/audit-logs
        [HttpGet("{id}/audit-logs")]
        public async Task<ActionResult<MedicalCaseAuditLogPagedResultDto>> GetAuditLogs(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var exists = await _db.MedicalCases.AsNoTracking().AnyAsync(m => m.Id == id && !m.IsDeleted);
            if (!exists) return NotFound();

            // Local mode returns empty audit logs (audit logs are server-side only)
            return new MedicalCaseAuditLogPagedResultDto
            {
                Logs = new List<MedicalCaseAuditLogDto>(),
                TotalCount = 0,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        // POST /api/medicalcases/{id}/print-logs
        [HttpPost("{id}/print-logs")]
        public async Task<IActionResult> AddPrintLog(Guid id, [FromBody] PrintLogInputDto request)
        {
            var mc = await _db.MedicalCases.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (mc == null) return NotFound();

            // Local mode: just acknowledge the print log (no persistent storage)
            return Ok(new { Success = true, Message = "打印日志已记录" });
        }

        // GET /api/medicalcases/{id}/consultations
        [HttpGet("{id}/consultations")]
        public async Task<ActionResult<List<ConsultationDetailDto>>> GetConsultationList(Guid id)
        {
            var exists = await _db.MedicalCases.AsNoTracking().AnyAsync(m => m.Id == id && !m.IsDeleted);
            if (!exists) return NotFound();

            var consultations = await _db.Consultations
                .AsNoTracking()
                .Where(c => c.Id == id) // shared key: Consultation.Id = MedicalCase.Id
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

                var dtos = consultations.Select(c => new ConsultationDetailDto
                {
                    Id = c.Id,
                    MedicalCaseId = c.Id, // shared key
                    PresentIllness = c.PresentIllness,
                    TongueDiagnosis = c.TongueDiagnosis,
                    PulseDiagnosis = c.PulseDiagnosis,
                    TcmDiagnosis = c.TcmDiagnosis,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToList();

            return Ok(dtos);
        }

        // GET /api/medicalcases/{id}/prescriptions
        [HttpGet("{id}/prescriptions")]
        public async Task<ActionResult<List<PrescriptionDetailDto>>> GetPrescriptionList(Guid id)
        {
            var exists = await _db.MedicalCases.AsNoTracking().AnyAsync(m => m.Id == id && !m.IsDeleted);
            if (!exists) return NotFound();

            var prescriptions = await _db.Prescriptions
                .Include(p => p.Items)
                .AsNoTracking()
                .Where(p => p.MedicalCaseId == id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var dtos = prescriptions.Select(p => new PrescriptionDetailDto
            {
                Id = p.Id,
                MedicalCaseId = p.MedicalCaseId,
                PrescriptionNumber = p.PrescriptionNumber,
                DosageCount = p.DosageCount,
                Discount = p.Discount,
                Usage = p.Usage,
                Advice = p.Advice,
                ReferencedFormulas = p.ReferencedFormulas,
                Remark = p.Remark,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Items = p.Items?.Select(item => new PrescriptionItemDto
                {
                    Id = item.Id,
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Dosage = item.Dosage,
                    Unit = item.Unit,
                    DecocteMethod = item.DecocteMethod,
                    UnitPrice = item.UnitPrice,
                    Usage = item.Usage,
                    Remark = item.Remark
                }).ToList() ?? new List<PrescriptionItemDto>()
            }).ToList();

            return Ok(dtos);
        }
    }
}
