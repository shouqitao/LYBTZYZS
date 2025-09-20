using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Module.Patients.Interfaces;

namespace LYBT.Module.Patients.Services;

/// <summary>
/// 患者服务 - UltraThink纯委托模式
/// 实现IPatientService接口，将请求路由到QueryService和BusinessService
/// </summary>
public class PatientService(
    IPatientQueryService queryService,
    IPatientBusinessService businessService) : IPatientService
{
    private readonly IPatientQueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
    private readonly IPatientBusinessService _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));

    #region 查询操作 - 委托给QueryService

    /// <inheritdoc/>
    public Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientSearchDto query)
    {
        // 转换为基础分页查询DTO
        var baseQuery = new PagedQueryBaseDto
        {
            PageIndex = query.PageIndex,
            PageSize = query.PageSize,
            Keyword = query.Keyword
        };
        return _queryService.GetPagedAsync(baseQuery);
    }

    /// <inheritdoc/>
    public Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
        => _queryService.GetByIdAsync(id);

    /// <inheritdoc/>
    public Task<ServiceResult<PatientDto>> GetByIdCardAsync(string idCard)
        => _queryService.GetByIdCardAsync(idCard);

    /// <inheritdoc/>
    public Task<ServiceResult<List<PatientDto>>> GetByPhoneAsync(string phone)
        => _queryService.GetByPhoneAsync(phone);

    /// <inheritdoc/>
    public Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword)
        => _queryService.SearchAsync(keyword);

    #endregion 查询操作 - 委托给QueryService

    #region 业务操作 - 委托给BusinessService

    /// <inheritdoc/>
    public Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
        => _businessService.CreateAsync(dto);

    /// <inheritdoc/>
    public Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
        => _businessService.UpdateAsync(id, dto);

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        var result = await _businessService.DeleteAsync(id);
        return result.IsSuccess 
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Failure(result.ErrorMessage ?? "删除患者失败");
    }

    /// <inheritdoc/>
    public async Task<ServiceResult> EnableAsync(Guid id)
    {
        var result = await _businessService.EnableAsync(new List<Guid> { id });
        return result.IsSuccess
            ? ServiceResult.Success()
            : ServiceResult.Failure(result.ErrorMessage ?? "启用患者失败");
    }

    /// <inheritdoc/>
    public async Task<ServiceResult> DisableAsync(Guid id)
    {
        var result = await _businessService.DisableAsync(new List<Guid> { id });
        return result.IsSuccess
            ? ServiceResult.Success()
            : ServiceResult.Failure(result.ErrorMessage ?? "禁用患者失败");
    }

    #endregion 业务操作 - 委托给BusinessService

    #region 批量操作 - 委托给BusinessService

    /// <inheritdoc/>
    public async Task<ServiceResult<object>> ImportPatientsAsync(List<PatientCreateDto> patients)
    {
        // 转换为PatientImportDto列表
        var importDtos = patients.Select(p => new PatientImportDto
        {
            Name = p.Name,
            GenderText = p.Gender == LYBT.Shared.Models.Enums.Gender.Male ? "男" : "女",
            BirthDateText = p.BirthDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            PhoneNumber = p.PhoneNumber,
            IdCardNumber = p.IdNumber,
            Address = p.Address,
            EmergencyContact = p.EmergencyContact,
            EmergencyPhone = p.EmergencyPhone,
            AllergyHistory = p.AllergyHistory
        }).ToList();

        var result = await _businessService.ImportPatientsAsync(importDtos);
        return result.IsSuccess
            ? ServiceResult<object>.Success(new { SuccessCount = result.Data?.Count ?? 0, ImportedPatients = result.Data })
            : ServiceResult<object>.Failure(result.ErrorMessage ?? "导入患者失败");
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<byte[]>> ExportPatientsAsync(PagedQueryBaseDto query)
    {
        var exportDto = new PatientExportDto { Name = query.Keyword ?? string.Empty };
        var result = await _businessService.ExportPatientsAsync(exportDto);
        
        if (!result.IsSuccess)
        {
            return ServiceResult<byte[]>.Failure(result.ErrorMessage ?? "导出患者失败");
        }

        // 转换为CSV字节数组
        var patients = result.Data ?? new List<PatientDto>();
        var csvContent = "姓名,性别,出生日期,手机号码,身份证号,地址\n";
        
        foreach (var patient in patients)
        {
            var gender = patient.Gender == LYBT.Shared.Models.Enums.Gender.Male ? "男" : "女";
            csvContent += $"{patient.Name},{gender},{patient.BirthDate:yyyy-MM-dd}," +
                         $"{patient.PhoneNumber},{patient.IdNumber},{patient.Address}\n";
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
        return ServiceResult<byte[]>.Success(bytes);
    }

    #endregion 批量操作 - 委托给BusinessService
}