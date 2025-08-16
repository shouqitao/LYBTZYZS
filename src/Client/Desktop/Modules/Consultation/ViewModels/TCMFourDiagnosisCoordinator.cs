using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Dialogs;
using LYBT.Desktop.Core.Extensions;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Desktop.Consultation.Components;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Consultation.ViewModels
{
    /// <summary>
    /// 中医四诊协调器 - UltraThink重构专门组件
    /// 专门负责协调各个专门组件，保持向后兼容性
    /// </summary>
    public class TCMFourDiagnosisCoordinator
    {
        #region 专门组件

        public TCMFourDiagnosisDataManager DataManager { get; }
        public TCMFourDiagnosisDescriptionGenerator DescriptionGenerator { get; }
        public TCMFourDiagnosisDataParser DataParser { get; }
        public TCMFourDiagnosisTemplateManager TemplateManager { get; }
        public TCMFourDiagnosisAnalyzer Analyzer { get; }

        #endregion

        #region 服务依赖

        private readonly IConsultationApiService _consultationApiService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<TCMFourDiagnosisCoordinator>? _logger;

        #endregion

        #region 命令

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand ClearCommand { get; }
        public DelegateCommand<string> QuickInputCommand { get; }
        public DelegateCommand AnalyzeSymptomsCommand { get; }
        public DelegateCommand LoadDataCommand { get; }

        #endregion

        #region 构造函数

        public TCMFourDiagnosisCoordinator(
            IConsultationApiService consultationApiService,
            IDialogService dialogService,
            ITCMDiagnosisAnalyzer? diagnosisAnalyzer = null,
            ILogger<TCMFourDiagnosisCoordinator>? logger = null)
        {
            _consultationApiService = consultationApiService ?? throw new ArgumentNullException(nameof(consultationApiService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger;

            // 初始化专门组件
            DataManager = new TCMFourDiagnosisDataManager();
            DescriptionGenerator = new TCMFourDiagnosisDescriptionGenerator();
            DataParser = new TCMFourDiagnosisDataParser();
            TemplateManager = new TCMFourDiagnosisTemplateManager();
            Analyzer = new TCMFourDiagnosisAnalyzer(null, DescriptionGenerator);

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), () => !DataManager.IsLoading);
            ClearCommand = new DelegateCommand(ExecuteClear);
            QuickInputCommand = new DelegateCommand<string>(ExecuteQuickInput);
            AnalyzeSymptomsCommand = new DelegateCommand(async () => await ExecuteAnalyzeSymptomsAsync());
            LoadDataCommand = new DelegateCommand(async () => await ExecuteLoadDataAsync());

            // 监听数据变化以更新命令可执行状态
            DataManager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DataManager.IsLoading))
                {
                    SaveCommand.RaiseCanExecuteChanged();
                }
            };

            _logger?.LogInformation("TCMFourDiagnosisCoordinator 初始化完成");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置看诊ID
        /// </summary>
        public void SetConsultationId(Guid consultationId)
        {
            DataManager.ConsultationId = consultationId;
            _logger?.LogInformation("设置看诊ID: {ConsultationId}", consultationId);
        }

        /// <summary>
        /// 获取完整的四诊描述数据
        /// </summary>
        public TCMFourDiagnosisData GetFourDiagnosisData()
        {
            return DescriptionGenerator.GenerateCompleteDescription(DataManager);
        }

        /// <summary>
        /// 映射到更新DTO
        /// </summary>
        public ConsultationUpdateDto MapToConsultationUpdateDto()
        {
            return new ConsultationUpdateDto
            {
                Inspection = DescriptionGenerator.GetInspectionDescription(DataManager),
                AuscultationOlfaction = DescriptionGenerator.GetAuscultationDescription(DataManager),
                Inquiry = DescriptionGenerator.GetInquiryDescription(DataManager),
                Palpation = DescriptionGenerator.GetPalpationDescription(DataManager),
                TongueInspection = DescriptionGenerator.GetTongueInspectionDescription(DataManager),
                PulseCondition = DescriptionGenerator.GetPulseConditionDescription(DataManager),
                TCMDiagnosis = DataManager.TCMSyndrome,
                TreatmentPrinciple = DataManager.TreatmentPrinciple,
                Remark = "由中医四诊系统生成"
            };
        }

        /// <summary>
        /// 从看诊详情加载数据
        /// </summary>
        public void LoadFromConsultationDetail(ConsultationDetailDto detail)
        {
            DataParser.ParseFromConsultationDetail(detail, DataManager);
            _logger?.LogInformation("从看诊详情加载数据完成");
        }

        /// <summary>
        /// 应用快速输入模板
        /// </summary>
        public bool ApplyQuickTemplate(string templateName)
        {
            var success = TemplateManager.ApplyTemplate(templateName, DataManager);
            if (success)
            {
                _logger?.LogInformation("应用模板: {TemplateName}", templateName);
            }
            return success;
        }

        /// <summary>
        /// 获取可用模板列表
        /// </summary>
        public IEnumerable<string> GetAvailableTemplates()
        {
            return TemplateManager.GetAvailableTemplates();
        }

        /// <summary>
        /// 分析诊断一致性
        /// </summary>
        public DiagnosisConsistencyResult AnalyzeDiagnosisConsistency()
        {
            return Analyzer.AnalyzeDiagnosisConsistency(DataManager);
        }

        /// <summary>
        /// 获取症状关键词分析
        /// </summary>
        public SymptomKeywordAnalysis GetSymptomAnalysis()
        {
            return Analyzer.AnalyzeSymptomKeywords(DataManager);
        }

        #endregion

        #region 命令执行方法

        /// <summary>
        /// 执行保存操作
        /// </summary>
        private async Task ExecuteSaveAsync()
        {
            if (DataManager.ConsultationId == Guid.Empty)
            {
                await _dialogService.ShowWarningAsync("请先选择要更新的看诊记录", "警告");
                return;
            }

            try
            {
                DataManager.IsLoading = true;
                _logger?.LogInformation("开始保存四诊信息");

                var dto = MapToConsultationUpdateDto();
                var response = await _consultationApiService.UpdateConsultationAsync(DataManager.ConsultationId, dto);

                if (response.IsSuccessStatusCode)
                {
                    await _dialogService.ShowInformationAsync("四诊信息保存成功", "成功");
                    _logger?.LogInformation("四诊信息保存成功");
                }
                else
                {
                    await _dialogService.ShowErrorAsync($"保存失败：{response.Error?.Content}", "错误");
                    _logger?.LogError("保存失败: {Error}", response.Error?.Content);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"保存失败：{ex.Message}", "错误");
                _logger?.LogError(ex, "保存四诊信息时发生异常");
            }
            finally
            {
                DataManager.IsLoading = false;
            }
        }

        /// <summary>
        /// 执行清空操作
        /// </summary>
        private void ExecuteClear()
        {
            DataManager.ClearAllData();
            _logger?.LogInformation("清空所有四诊数据");
        }

        /// <summary>
        /// 执行快速输入
        /// </summary>
        private void ExecuteQuickInput(string? templateName)
        {
            if (string.IsNullOrWhiteSpace(templateName)) return;

            var success = ApplyQuickTemplate(templateName);
            if (!success)
            {
                _logger?.LogWarning("应用模板失败: {TemplateName}", templateName);
            }
        }

        /// <summary>
        /// 执行症状分析
        /// </summary>
        private async Task ExecuteAnalyzeSymptomsAsync()
        {
            try
            {
                DataManager.IsLoading = true;
                _logger?.LogInformation("开始智能症状分析");

                var recommendations = await Analyzer.AnalyzeSyndromeAsync(DataManager);
                
                DataManager.RecommendedSyndromes.Clear();
                foreach (var syndrome in recommendations.Take(5))
                {
                    DataManager.RecommendedSyndromes.Add(syndrome);
                }

                if (DataManager.RecommendedSyndromes.Any())
                {
                    var recommendationsText = string.Join("、", DataManager.RecommendedSyndromes);
                    await _dialogService.ShowInformationAsync(
                        $"基于当前症状，推荐证型：{recommendationsText}", 
                        "智能诊断推荐");
                    
                    _logger?.LogInformation("智能分析完成，推荐{Count}个证型", DataManager.RecommendedSyndromes.Count);
                }
                else
                {
                    await _dialogService.ShowInformationAsync("暂无明确的证型推荐，请补充更多症状信息", "智能诊断推荐");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"智能分析失败：{ex.Message}", "错误");
                _logger?.LogError(ex, "智能症状分析时发生异常");
            }
            finally
            {
                DataManager.IsLoading = false;
            }
        }

        /// <summary>
        /// 执行数据加载
        /// </summary>
        private async Task ExecuteLoadDataAsync()
        {
            if (DataManager.ConsultationId == Guid.Empty) return;

            try
            {
                DataManager.IsLoading = true;
                _logger?.LogInformation("开始加载看诊数据: {ConsultationId}", DataManager.ConsultationId);

                var response = await _consultationApiService.GetByIdAsync(DataManager.ConsultationId);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    LoadFromConsultationDetail(response.Content);
                    _logger?.LogInformation("看诊数据加载完成");
                }
                else
                {
                    _logger?.LogWarning("加载看诊数据失败: {Error}", response.Error?.Content);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"加载数据失败：{ex.Message}", "错误");
                _logger?.LogError(ex, "加载看诊数据时发生异常");
            }
            finally
            {
                DataManager.IsLoading = false;
            }
        }

        #endregion
    }
}