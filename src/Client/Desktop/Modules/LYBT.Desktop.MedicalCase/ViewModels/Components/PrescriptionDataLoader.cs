using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.MedicalCase.ViewModels.Components;

/// <summary>
/// 处方数据加载器
/// 负责加载药材、验方、历史处方等数据
/// OpenSpec: cleanup-ui-layer - Phase 1.1 PrescriptionPanelViewModel拆分
/// </summary>
public class PrescriptionDataLoader
{
    #region 字段

    private readonly IHerbRepository _herbRepository;
    private readonly IFormulaRepository _formulaRepository;
    private readonly ILogger<PrescriptionDataLoader> _logger;

    #endregion

    #region 常量

    /// <summary>
    /// 分页大小（使用系统最大值，用于批量加载场景）
    /// </summary>
    private static int PageSize => SystemConstants.MaxPageSize;

    #endregion

    #region 构造函数

    public PrescriptionDataLoader(
        IHerbRepository herbRepository,
        IFormulaRepository formulaRepository,
        ILoggerFactory loggerFactory)
    {
        _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
        _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));
        _logger = loggerFactory.CreateLogger<PrescriptionDataLoader>();
    }

    #endregion

    #region 加载药材

    /// <summary>
    /// 加载所有药材（分页加载）
    /// </summary>
    /// <param name="targetCollection">目标集合</param>
    /// <returns>加载的药材数量</returns>
    public async Task<int> LoadAllHerbsAsync(ObservableCollection<HerbDto> targetCollection)
    {
        try
        {
            targetCollection.Clear();
            int page = 1;
            int totalLoaded = 0;

            while (true)
            {
                var result = await _herbRepository.GetPagedAsync(page: page, pageSize: PageSize);
                if (result.Items == null || !result.Items.Any())
                    break;

                foreach (var herb in result.Items)
                {
                    targetCollection.Add(herb);
                }
                totalLoaded += result.Items.Count;

                // 如果返回的数量少于请求的数量，说明已经是最后一页
                if (result.Items.Count < PageSize || totalLoaded >= result.TotalCount)
                    break;

                page++;
            }

            _logger.LogInformation("加载药材列表完成，共{Count}种", totalLoaded);
            return totalLoaded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载药材列表失败");
            return 0;
        }
    }

    /// <summary>
    /// 将药材列表注入到所有药材项
    /// </summary>
    /// <param name="herbItems">药材项集合</param>
    /// <param name="allHerbs">所有药材列表</param>
    public void InjectHerbsToItems(
        ObservableCollection<PrescriptionHerbItemViewModel> herbItems,
        ObservableCollection<HerbDto> allHerbs)
    {
        foreach (var item in herbItems)
        {
            item.AllHerbs = allHerbs;
        }
    }

    #endregion

    #region 加载验方

    /// <summary>
    /// 加载验方列表
    /// </summary>
    /// <param name="targetCollection">目标集合</param>
    /// <returns>加载的验方数量</returns>
    public async Task<int> LoadFormulasAsync(ObservableCollection<FormulaDto> targetCollection)
    {
        try
        {
            var result = await _formulaRepository.GetPagedAsync(page: 1, pageSize: PageSize);
            targetCollection.Clear();

            foreach (var formula in result.Items)
            {
                targetCollection.Add(formula);
            }

            _logger.LogInformation("加载验方列表完成，共{Count}个", targetCollection.Count);
            return targetCollection.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载验方列表失败");
            return 0;
        }
    }

    #endregion

    #region 加载历史处方

    /// <summary>
    /// 加载历史处方结果
    /// </summary>
    public class LoadHistoryResult
    {
        public bool IsSuccess { get; init; }
        public int Count { get; init; }
        public string? ErrorMessage { get; init; }

        public static LoadHistoryResult Success(int count) => new() { IsSuccess = true, Count = count };
        public static LoadHistoryResult Failed(string message) => new() { IsSuccess = false, ErrorMessage = message };
        public static LoadHistoryResult Empty() => new() { IsSuccess = true, Count = 0 };
    }

    /// <summary>
    /// 加载患者的历史处方
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <param name="currentMedicalCaseId">当前医案ID（排除）</param>
    /// <param name="targetCollection">目标集合</param>
    /// <param name="medicalCaseRepository">医案仓储</param>
    /// <returns>加载结果</returns>
    public async Task<LoadHistoryResult> LoadPrescriptionHistoryAsync(
        Guid patientId,
        Guid currentMedicalCaseId,
        ObservableCollection<MedicalCaseDto> targetCollection,
        Interfaces.IMedicalCaseRepository medicalCaseRepository)
    {
        try
        {
            if (patientId == Guid.Empty)
            {
                _logger.LogWarning("PatientId为空，无法加载历史处方");
                return LoadHistoryResult.Empty();
            }

            var cases = await medicalCaseRepository.GetByPatientIdAsync(patientId);
            targetCollection.Clear();

            // 过滤掉当前医案，只显示其他历史医案
            var historyCases = cases
                .Where(c => c.Id != currentMedicalCaseId)
                .OrderByDescending(c => c.ConsultationDate);

            foreach (var caseItem in historyCases)
            {
                targetCollection.Add(caseItem);
            }

            _logger.LogInformation("加载历史处方完成，共{Count}条", targetCollection.Count);
            return LoadHistoryResult.Success(targetCollection.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载历史处方失败");
            return LoadHistoryResult.Failed(ex.Message);
        }
    }

    #endregion
}
