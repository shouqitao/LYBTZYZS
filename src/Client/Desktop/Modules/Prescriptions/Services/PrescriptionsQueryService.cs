using LYBT.Desktop.Prescriptions.Interfaces;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.Services;

/// <summary>
/// 处方管理查询服务 - UltraThink双层架构查询专业层
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 职责：处方管理复杂查询、搜索过滤、统计报表、配伍历史检索
/// 提供只读查询操作，不涉及数据修改，专注处方记录检索和统计分析
/// 集成企业级日志记录，支持处方管理和档案查询需求
/// 适配中医诊所处方管理查询场景，确保查询性能和数据安全性
/// </summary>
public class PrescriptionsQueryService(
    ILogger<PrescriptionsQueryService> logger,
    IPrescriptionApi prescriptionApi) : IPrescriptionsQueryService
{
    private readonly ILogger<PrescriptionsQueryService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IPrescriptionApi _prescriptionApi = prescriptionApi ?? throw new ArgumentNullException(nameof(prescriptionApi));

    /// <summary>
    /// 分页查询处方档案列表
    /// 基于查询条件执行处方分页检索，支持过滤和排序
    /// </summary>
    /// <param name="query">分页查询参数</param>
    /// <returns>包含处方列表和总数的分页结果</returns>
    public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query)
    {
        try
        {
            _logger.LogDebug(
                "执行处方分页查询，页码: {PageNumber}, 页大小: {PageSize}",
                query.PageIndex, query.PageSize);

            var refitResponse = await _prescriptionApi.GetListAsync(
                query.PageIndex,
                query.PageSize,
                query.Keyword);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var result = refitResponse.Content;
                _logger.LogDebug("处方分页查询成功，总数: {TotalCount}, 当前页数据数: {ItemCount}",
                    result.TotalCount, result.Items.Count);
                return ServiceResult<PagedResult<PrescriptionDto>>.Success(result, "查询成功");
            }

            _logger.LogWarning(
                "处方分页查询HTTP请求失败, 状态码: {StatusCode}",
                refitResponse.StatusCode);
            return ServiceResult<PagedResult<PrescriptionDto>>.Failure("查询处方列表网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处方分页查询异常");
            return ServiceResult<PagedResult<PrescriptionDto>>.Failure("查询处方列表失败");
        }
    }

    /// <summary>
    /// 根据处方ID获取详细档案
    /// 查询指定处方的完整档案信息，包含药材配伍和价格明细
    /// </summary>
    /// <param name="id">处方唯一标识</param>
    /// <returns>处方详细档案DTO</returns>
    public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("查询处方详细档案: {PrescriptionId}", id);

            var refitResponse = await _prescriptionApi.GetByIdAsync(id);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var detailDto = refitResponse.Content;
                // PrescriptionDetailDto 继承自 PrescriptionDto，可以直接使用
                // 但需要转换为基类类型以避免额外的详情字段
                var prescriptionDto = new PrescriptionDto
                {
                    Id = detailDto.Id,
                    MedicalCaseId = detailDto.MedicalCaseId,
                    PatientId = detailDto.PatientId,
                    UserId = detailDto.UserId,
                    PatientName = detailDto.PatientName,
                    DoctorName = detailDto.DoctorName,
                    Diagnosis = detailDto.Diagnosis,
                    Usage = detailDto.Usage,
                    Indication = detailDto.Indication,
                    DosageCount = detailDto.DosageCount,
                    Discount = detailDto.Discount,
                    Advice = detailDto.Advice,
                    FormulaSource = detailDto.FormulaSource,
                    Items = detailDto.Items,
                    DosageForm = detailDto.DosageForm,
                    PrescriptionNo = detailDto.PrescriptionNo,
                    Status = detailDto.Status,
                    CreateTime = detailDto.CreateTime,
                    UpdateTime = detailDto.UpdateTime,
                    Remark = detailDto.Remark
                };

                _logger.LogInformation("处方详情查询成功: {PrescriptionId}", prescriptionDto.Id);
                return ServiceResult<PrescriptionDto>.Success(prescriptionDto, "处方详情查询成功");
            }
            else
            {
                var errorMessage = $"处方详情查询失败: {refitResponse.ReasonPhrase}";
                _logger.LogError(errorMessage);
                return ServiceResult<PrescriptionDto>.Failure(errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询处方详情异常: {PrescriptionId}", id);
            return ServiceResult<PrescriptionDto>.Failure($"查询处方详情失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 关键字搜索处方记录
    /// 基于关键字执行模糊搜索，支持患者姓名、处方名称、药材名称匹配
    /// </summary>
    /// <param name="keyword">搜索关键字</param>
    /// <returns>匹配处方记录列表</returns>
    public async Task<ServiceResult<List<PrescriptionDto>>> Search(string keyword)
    {
        try
        {
            _logger.LogDebug("处方关键字搜索: {Keyword}", keyword);

            if (string.IsNullOrWhiteSpace(keyword))
            {
                return ServiceResult<List<PrescriptionDto>>.Success([]);
            }

            // 使用分页查询API进行搜索
            var refitResponse = await _prescriptionApi.GetListAsync(
                page: 1,
                pageSize: 100, // 搜索结果限制为100条
                keyword: keyword);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var searchResults = refitResponse.Content.Items.ToList();
                _logger.LogDebug("处方关键字搜索成功: {Keyword}, 结果数: {Count}", keyword, searchResults.Count);
                return ServiceResult<List<PrescriptionDto>>.Success(searchResults, "搜索成功");
            }

            _logger.LogWarning("处方搜索HTTP请求失败: {Keyword}, 状态码: {StatusCode}", keyword, refitResponse.StatusCode);
            return ServiceResult<List<PrescriptionDto>>.Success([], "搜索网络请求失败，返回空结果");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处方搜索异常: {Keyword}", keyword);
            return ServiceResult<List<PrescriptionDto>>.Failure($"处方搜索失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取处方统计数据
    /// 生成处方管理相关的基础统计信息和报表数据
    /// </summary>
    /// <returns>处方统计信息DTO</returns>
    public async Task<ServiceResult<PrescriptionStatisticsDto>> GetStatisticsAsync()
    {
        try
        {
            _logger.LogDebug("生成处方管理统计数据");

            // 使用分页查询获取数据来生成统计
            var allDataResponse = await _prescriptionApi.GetListAsync(1, 10000); // 获取大量数据用于统计
            
            var stats = new PrescriptionStatisticsDto();
            
            if (allDataResponse.IsSuccessStatusCode && allDataResponse.Content != null)
            {
                var prescriptions = allDataResponse.Content.Items;
                
                // 基于实际枚举值进行统计计算
                stats.TotalCount = allDataResponse.Content.TotalCount;
                stats.DraftCount = prescriptions.Count(p => p.Status == CommonStatus.Disabled); // 草稿状态
                stats.CompletedCount = prescriptions.Count(p => p.Status == CommonStatus.Enabled); // 完成状态
                stats.TotalAmount = prescriptions.Sum(p => p.TotalAmount);
                stats.AverageAmount = stats.TotalCount > 0 ? stats.TotalAmount / stats.TotalCount : 0;
                
                _logger.LogDebug("处方统计数据生成成功: 总数 {Total}, 草稿 {Draft}, 完成 {Completed}", 
                    stats.TotalCount, stats.DraftCount, stats.CompletedCount);
            }
            else
            {
                _logger.LogWarning("获取处方数据用于统计失败，使用默认空统计");
            }

            return ServiceResult<PrescriptionStatisticsDto>.Success(stats, "统计数据生成成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处方统计数据生成异常");
            return ServiceResult<PrescriptionStatisticsDto>.Failure($"生成统计数据失败: {ex.Message}");
        }
    }
}
