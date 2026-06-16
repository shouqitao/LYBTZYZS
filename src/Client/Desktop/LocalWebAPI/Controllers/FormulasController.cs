using System.Security.Claims;
using LYBT.Infrastructure.Web;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FormulasController : BaseApiController
{
    private readonly IFormulaService _formulaService;
    private readonly IFormulaImportExportService _importExportService;

    public FormulasController(
        IFormulaService formulaService,
        IFormulaImportExportService importExportService,
        ILogger<FormulasController> logger) : base(logger)
    {
        _formulaService = formulaService;
        _importExportService = importExportService;
    }

    private Guid GetCurrentUserId()
        => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] string? keyword = null)
    {
        var result = await _formulaService.SearchAsync(keyword ?? "");
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _formulaService.GetByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FormulaInputDto dto)
    {
        var result = await _formulaService.CreateAsync(dto, GetCurrentUserId());
        return HandleResult(result, "创建成功");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] FormulaInputDto dto)
    {
        var result = await _formulaService.UpdateAsync(id, dto);
        return HandleResult(result, "更新成功");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _formulaService.DeleteAsync(id);
        return HandleResult(result, "删除成功");
    }

    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto request)
    {
        if (request?.Ids == null || request.Ids.Count == 0)
            return ValidationFail("ids 不能为空");
        var result = await _formulaService.BatchDeleteAsync(request.Ids, GetCurrentUserId());
        return HandleResult(result);
    }

    [HttpPost("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var result = await _formulaService.ToggleStatusAsync(id);
        return HandleResult(result);
    }

    [HttpPost("{id}/clone")]
    public async Task<IActionResult> Clone(Guid id)
    {
        var source = await _formulaService.GetByIdAsync(id);
        if (!source.IsSuccess || source.Data == null)
            return HandleResult(source);

        var clone = new FormulaInputDto
        {
            Name = $"{source.Data.Name} (副本)",
            Effect = source.Data.Effect,
            Description = source.Data.Description,
            Usage = source.Data.Usage,
            Property = source.Data.Property,
            Category = source.Data.Category,
            IsShared = false,
            Remark = source.Data.Remark,
            Herbs = source.Data.Herbs?.Select(h => new FormulaHerbItemInputDto
            {
                HerbId = h.HerbId,
                HerbName = h.HerbName,
                Dosage = h.Dosage,
                Unit = h.Unit,
                Usage = h.Usage,
                DecocteMethod = h.DecocteMethod
            }).ToList() ?? new()
        };

        var result = await _formulaService.CreateAsync(clone, GetCurrentUserId());
        return HandleResult(result, "复制成功");
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string? keyword = null)
    {
        var result = await _formulaService.SearchAsync(keyword ?? "");
        return HandleResult(result);
    }

    [HttpGet("import-template")]
    [AllowAnonymous]
    public IActionResult ExportTemplate()
    {
        var stream = _importExportService.GenerateImportTemplate();
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "formulas-template.xlsx");
    }

    [HttpPost("batch-import")]
    public async Task<IActionResult> BatchImport([FromBody] List<FormulaImportItemDto> formulas)
    {
        if (formulas == null || formulas.Count == 0)
            return ValidationFail("导入列表不能为空");
        var result = await _importExportService.ImportFromDataAsync(formulas);
        return HandleResult(result);
    }

    [HttpGet("pending-validation")]
    public async Task<IActionResult> GetPendingValidation()
    {
        var result = await _formulaService.GetPendingValidationFormulasAsync();
        return HandleResult(result);
    }

    [HttpPost("{formulaId}/herbs/{herbItemId}/validate")]
    public async Task<IActionResult> ValidateHerb(Guid formulaId, Guid herbItemId, [FromBody] ValidateHerbRequest request)
    {
        var result = await _formulaService.ValidateFormulaHerbAsync(formulaId, herbItemId, request.SelectedHerbId);
        return HandleResult(result);
    }
}

public class ValidateHerbRequest
{
    public Guid SelectedHerbId { get; set; }
}
