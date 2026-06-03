using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;

namespace LYBT.LocalWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FormulasController : ControllerBase
    {
        private readonly LocalWebApiDbContext _db;

        public FormulasController(LocalWebApiDbContext db)
        {
            _db = db;
        }

        // GET /api/formulas?keyword=&category=
        [HttpGet]
        public async Task<ActionResult<List<Formula>>> GetFormulas([FromQuery] string keyword, [FromQuery] string? category = null)
        {
            var q = _db.Formulas.AsNoTracking().Where(f => !f.IsDeleted);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                q = q.Where(f => f.Name.Contains(keyword));
            }
            if (!string.IsNullOrWhiteSpace(category))
            {
                q = q.Where(f => f.Category == category);
            }
            return await q.ToListAsync();
        }

        // GET /api/formulas/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Formula>> GetFormula(Guid id)
        {
            var formula = await _db.Formulas.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
            if (formula == null) return NotFound();
            return formula;
        }

        // POST /api/formulas
        [HttpPost]
        public async Task<IActionResult> CreateFormula([FromBody] Formula formula)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _db.Formulas.Add(formula);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetFormula), new { id = formula.Id }, formula);
        }

        // PUT /api/formulas/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFormula(Guid id, [FromBody] Formula updated)
        {
            if (id != updated.Id) return BadRequest("ID mismatch between URL and payload.");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var existing = await _db.Formulas.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            _db.Entry(existing).CurrentValues.SetValues(updated);
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        // DELETE /api/formulas/{id} -> soft delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFormula(Guid id)
        {
            var existing = await _db.Formulas.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            existing.IsDeleted = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST /api/formulas/batch-delete
        [HttpPost("batch-delete")]
        public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto request)
        {
            if (request?.Ids == null || request.Ids.Count == 0) return BadRequest("ids is required.");
            var formulas = await _db.Formulas.Where(f => request.Ids.Contains(f.Id) && !f.IsDeleted).ToListAsync();
            foreach (var f in formulas) f.IsDeleted = true;
            await _db.SaveChangesAsync();
            return Ok(new { count = formulas.Count });
        }

        // POST /api/formulas/batch-enable
        [HttpPost("batch-enable")]
        public async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto request)
        {
            if (request?.Ids == null || request.Ids.Count == 0) return BadRequest("ids is required.");
            var formulas = await _db.Formulas.Where(f => request.Ids.Contains(f.Id) && !f.IsDeleted).ToListAsync();
            foreach (var f in formulas) f.Status = CommonStatus.Enabled;
            await _db.SaveChangesAsync();
            return Ok(new { count = formulas.Count });
        }

        // POST /api/formulas/batch-disable
        [HttpPost("batch-disable")]
        public async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto request)
        {
            if (request?.Ids == null || request.Ids.Count == 0) return BadRequest("ids is required.");
            var formulas = await _db.Formulas.Where(f => request.Ids.Contains(f.Id) && !f.IsDeleted).ToListAsync();
            foreach (var f in formulas) f.Status = CommonStatus.Disabled;
            await _db.SaveChangesAsync();
            return Ok(new { count = formulas.Count });
        }

        // POST /api/formulas/{id}/toggle-status
        [HttpPost("{id}/toggle-status")]
        public async Task<ActionResult<Formula>> ToggleStatus(Guid id)
        {
            var formula = await _db.Formulas.FindAsync(id);
            if (formula == null || formula.IsDeleted) return NotFound();
            formula.Status = formula.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled;
            await _db.SaveChangesAsync();
            return Ok(formula);
        }

        // POST /api/formulas/{id}/restore
        [HttpPost("{id}/restore")]
        public async Task<ActionResult<Formula>> Restore(Guid id)
        {
            var formula = await _db.Formulas.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == id);
            if (formula == null) return NotFound();
            if (!formula.IsDeleted) return BadRequest("Formula is not deleted.");
            formula.IsDeleted = false;
            await _db.SaveChangesAsync();
            return Ok(formula);
        }

        // POST /api/formulas/{id}/clone
        [HttpPost("{id}/clone")]
        public async Task<ActionResult<Formula>> Clone(Guid id)
        {
            var source = await _db.Formulas
                .AsNoTracking()
                .Include(f => f.Herbs)
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
            if (source == null) return NotFound();

            var clone = new Formula
            {
                Id = Guid.NewGuid(),
                Name = source.Name,
                Effect = source.Effect,
                Indication = source.Indication,
                Usage = source.Usage,
                Remark = source.Remark,
                Property = source.Property,
                Status = source.Status,
                IsShared = source.IsShared,
                ValidationStatus = FormulaValidationStatus.Draft,
                Category = source.Category,
                FormulaType = source.FormulaType,
                UserId = source.UserId,
                IsDeleted = false,
                Herbs = source.Herbs?.Select(h => new FormulaHerbItem
                {
                    Id = Guid.NewGuid(),
                    HerbId = h.HerbId,
                    HerbName = h.HerbName,
                    Dosage = h.Dosage,
                    Unit = h.Unit,
                    Remark = h.Remark,
                    OriginalHerbName = h.OriginalHerbName,
                    IsValidated = h.IsValidated,
                    Usage = h.Usage,
                    ProcessingMethod = h.ProcessingMethod,
                    DecocteMethod = h.DecocteMethod
                }).ToList() ?? new List<FormulaHerbItem>()
            };

            _db.Formulas.Add(clone);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetFormula), new { id = clone.Id }, clone);
        }

        // GET /api/formulas/export
        [HttpGet("export")]
        public async Task<ActionResult<List<Formula>>> Export()
        {
            var formulas = await _db.Formulas
                .AsNoTracking()
                .Where(f => !f.IsDeleted)
                .Include(f => f.Herbs)
                .ToListAsync();
            return Ok(formulas);
        }

        // GET /api/formulas/import-template
        [HttpGet("import-template")]
        public IActionResult ExportTemplate()
        {
            var template = new[]
            {
                new { Name = "", Effect = "", Indication = "", Usage = "", Category = "", FormulaType = "", Remark = "" }
            };
            return Ok(template);
        }

        // POST /api/formulas/batch-import
        [HttpPost("batch-import")]
        public async Task<ActionResult<FormulaBatchImportResultDto>> Import([FromBody] FormulaBatchImportInputDto request)
        {
            if (request == null || request.Formulas == null || request.Formulas.Count == 0)
                return BadRequest("验方列表不能为空");

            var result = new FormulaBatchImportResultDto
            {
                FileName = request.FileName,
                ImportTime = DateTime.UtcNow,
                StartTime = DateTime.UtcNow,
                TotalCount = request.Formulas.Count
            };

            foreach (var dto in request.Formulas)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(dto.Name))
                    {
                        result.FailureCount++;
                        result.Failures.Add(new FormulaImportFailureDto
                        {
                            FormulaName = dto.Name ?? string.Empty,
                            ErrorMessage = "验方名称不能为空"
                        });
                        continue;
                    }

                    var formula = new Formula
                    {
                        Id = Guid.NewGuid(),
                        Name = dto.Name,
                        Effect = dto.Effect,
                        Usage = dto.Usage,
                        Property = dto.Property,
                        IsShared = dto.IsShared,
                        Remark = dto.Remark,
                        Status = CommonStatus.Enabled,
                        ValidationStatus = FormulaValidationStatus.Draft,
                        CreatedAt = DateTime.UtcNow,
                        Herbs = new List<FormulaHerbItem>()
                    };

                    foreach (var herbDto in dto.Herbs)
                    {
                        formula.Herbs.Add(new FormulaHerbItem
                        {
                            Id = Guid.NewGuid(),
                            HerbName = herbDto.HerbName,
                            OriginalHerbName = herbDto.HerbName,
                            Dosage = herbDto.Dosage,
                            Unit = herbDto.Unit ?? string.Empty,
                            Usage = herbDto.Usage,
                            ProcessingMethod = herbDto.Preparation
                        });
                    }

                    _db.Formulas.Add(formula);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Failures.Add(new FormulaImportFailureDto
                    {
                        FormulaName = dto.Name ?? string.Empty,
                        ErrorMessage = ex.Message
                    });
                }
            }

            result.EndTime = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(result);
        }

        // GET /api/formulas/pending-validation (P1: align with Server)
        [HttpGet("pending-validation")]
        public async Task<ActionResult<List<Formula>>> GetPendingValidation()
        {
            var formulas = await _db.Formulas
                .AsNoTracking()
                .Include(f => f.Herbs)
                .Where(f => !f.IsDeleted
                    && f.IsShared
                    && f.ValidationStatus != FormulaValidationStatus.Validated)
                .ToListAsync();
            return Ok(formulas);
        }

        // POST /api/formulas/{formulaId}/herbs/{herbItemId}/validate (P1: align with Server)
        [HttpPost("{formulaId}/herbs/{herbItemId}/validate")]
        public async Task<IActionResult> ValidateHerb(
            Guid formulaId,
            Guid herbItemId,
            [FromBody] ValidateFormulaHerbInputDto request)
        {
            if (request?.SelectedHerbId == null || request.SelectedHerbId == Guid.Empty)
                return BadRequest("SelectedHerbId is required.");

            var formula = await _db.Formulas
                .Include(f => f.Herbs)
                .FirstOrDefaultAsync(f => f.Id == formulaId && !f.IsDeleted);
            if (formula == null) return NotFound("验方不存在");

            var herbItem = formula.Herbs?.FirstOrDefault(h => h.Id == herbItemId);
            if (herbItem == null) return NotFound("药材项不存在");

            var targetHerb = await _db.Herbs.AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == request.SelectedHerbId && !h.IsDeleted);
            if (targetHerb == null) return NotFound("系统药材不存在");

            herbItem.HerbId = request.SelectedHerbId;
            herbItem.HerbName = targetHerb.Name;
            herbItem.IsValidated = true;

            // Update formula validation status if all herbs validated
            if (formula.Herbs != null && formula.Herbs.All(h => h.IsValidated))
            {
                formula.ValidationStatus = FormulaValidationStatus.Validated;
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "药材验证成功" });
        }

        // GET /api/formulas/categories
        [HttpGet("categories")]
        public async Task<ActionResult<List<string>>> GetCategories()
        {
            var categories = await _db.Formulas
                .AsNoTracking()
                .Where(f => !f.IsDeleted && f.Category != null)
                .Select(f => f.Category!)
                .Distinct()
                .ToListAsync();
            return Ok(categories);
        }
    }
}
