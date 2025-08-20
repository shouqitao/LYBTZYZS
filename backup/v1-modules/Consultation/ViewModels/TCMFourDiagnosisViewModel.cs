using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using AutoMapper;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Consultation.Components;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Consultation.ViewModels
{
    /// <summary>
    /// 中医四诊视图模型 - UltraThink重构（协调器模式）
    /// 保持向后兼容性，将所有职责委托给专门组件
    /// </summary>
    public class TCMFourDiagnosisViewModel : BindableBase
    {
        private readonly TCMFourDiagnosisCoordinator _coordinator;
        private readonly IMapper _mapper;

        #region 委托属性（保持向后兼容）

        public Guid ConsultationId
        {
            get => _coordinator.DataManager.ConsultationId;
            set 
            { 
                _coordinator.DataManager.ConsultationId = value;
                RaisePropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _coordinator.DataManager.IsLoading;
            set 
            { 
                _coordinator.DataManager.IsLoading = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<string> RecommendedSyndromes => _coordinator.DataManager.RecommendedSyndromes;

        #endregion

        #region 望诊属性（委托给DataManager）

        public string Complexion
        {
            get => _coordinator.DataManager.Complexion;
            set 
            { 
                _coordinator.DataManager.Complexion = value;
                RaisePropertyChanged();
            }
        }

        public string Spirit
        {
            get => _coordinator.DataManager.Spirit;
            set 
            { 
                _coordinator.DataManager.Spirit = value;
                RaisePropertyChanged();
            }
        }

        public string BodyShape
        {
            get => _coordinator.DataManager.BodyShape;
            set 
            { 
                _coordinator.DataManager.BodyShape = value;
                RaisePropertyChanged();
            }
        }

        public string TongueBody
        {
            get => _coordinator.DataManager.TongueBody;
            set 
            { 
                _coordinator.DataManager.TongueBody = value;
                RaisePropertyChanged();
            }
        }

        public string TongueCoating
        {
            get => _coordinator.DataManager.TongueCoating;
            set 
            { 
                _coordinator.DataManager.TongueCoating = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region 闻诊属性（委托给DataManager）

        public string Voice
        {
            get => _coordinator.DataManager.Voice;
            set 
            { 
                _coordinator.DataManager.Voice = value;
                RaisePropertyChanged();
            }
        }

        public string Breath
        {
            get => _coordinator.DataManager.Breath;
            set 
            { 
                _coordinator.DataManager.Breath = value;
                RaisePropertyChanged();
            }
        }

        public string Cough
        {
            get => _coordinator.DataManager.Cough;
            set 
            { 
                _coordinator.DataManager.Cough = value;
                RaisePropertyChanged();
            }
        }

        public string Odor
        {
            get => _coordinator.DataManager.Odor;
            set 
            { 
                _coordinator.DataManager.Odor = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region 问诊属性（委托给DataManager）

        public string ChiefComplaint
        {
            get => _coordinator.DataManager.ChiefComplaint;
            set 
            { 
                _coordinator.DataManager.ChiefComplaint = value;
                RaisePropertyChanged();
            }
        }

        public string PresentIllness
        {
            get => _coordinator.DataManager.PresentIllness;
            set 
            { 
                _coordinator.DataManager.PresentIllness = value;
                RaisePropertyChanged();
            }
        }

        public string ColdHeat
        {
            get => _coordinator.DataManager.ColdHeat;
            set 
            { 
                _coordinator.DataManager.ColdHeat = value;
                RaisePropertyChanged();
            }
        }

        public string Sweat
        {
            get => _coordinator.DataManager.Sweat;
            set 
            { 
                _coordinator.DataManager.Sweat = value;
                RaisePropertyChanged();
            }
        }

        public string HeadBody
        {
            get => _coordinator.DataManager.HeadBody;
            set 
            { 
                _coordinator.DataManager.HeadBody = value;
                RaisePropertyChanged();
            }
        }

        public string ChestAbdomen
        {
            get => _coordinator.DataManager.ChestAbdomen;
            set 
            { 
                _coordinator.DataManager.ChestAbdomen = value;
                RaisePropertyChanged();
            }
        }

        public string Appetite
        {
            get => _coordinator.DataManager.Appetite;
            set 
            { 
                _coordinator.DataManager.Appetite = value;
                RaisePropertyChanged();
            }
        }

        public string StoolUrine
        {
            get => _coordinator.DataManager.StoolUrine;
            set 
            { 
                _coordinator.DataManager.StoolUrine = value;
                RaisePropertyChanged();
            }
        }

        public string Sleep
        {
            get => _coordinator.DataManager.Sleep;
            set 
            { 
                _coordinator.DataManager.Sleep = value;
                RaisePropertyChanged();
            }
        }

        public string Menstruation
        {
            get => _coordinator.DataManager.Menstruation;
            set 
            { 
                _coordinator.DataManager.Menstruation = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region 切诊属性（委托给DataManager）

        public string PulseRate
        {
            get => _coordinator.DataManager.PulseRate;
            set 
            { 
                _coordinator.DataManager.PulseRate = value;
                RaisePropertyChanged();
            }
        }

        public string PulseRhythm
        {
            get => _coordinator.DataManager.PulseRhythm;
            set 
            { 
                _coordinator.DataManager.PulseRhythm = value;
                RaisePropertyChanged();
            }
        }

        public string PulseStrength
        {
            get => _coordinator.DataManager.PulseStrength;
            set 
            { 
                _coordinator.DataManager.PulseStrength = value;
                RaisePropertyChanged();
            }
        }

        public string PulseShape
        {
            get => _coordinator.DataManager.PulseShape;
            set 
            { 
                _coordinator.DataManager.PulseShape = value;
                RaisePropertyChanged();
            }
        }

        public string LeftPulse
        {
            get => _coordinator.DataManager.LeftPulse;
            set 
            { 
                _coordinator.DataManager.LeftPulse = value;
                RaisePropertyChanged();
            }
        }

        public string RightPulse
        {
            get => _coordinator.DataManager.RightPulse;
            set 
            { 
                _coordinator.DataManager.RightPulse = value;
                RaisePropertyChanged();
            }
        }

        public string Palpation
        {
            get => _coordinator.DataManager.Palpation;
            set 
            { 
                _coordinator.DataManager.Palpation = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region 综合诊断（委托给DataManager）

        public string TCMSyndrome
        {
            get => _coordinator.DataManager.TCMSyndrome;
            set 
            { 
                _coordinator.DataManager.TCMSyndrome = value;
                RaisePropertyChanged();
            }
        }

        public string TreatmentPrinciple
        {
            get => _coordinator.DataManager.TreatmentPrinciple;
            set 
            { 
                _coordinator.DataManager.TreatmentPrinciple = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region 常用选项集合（委托给DataManager）

        public ObservableCollection<string> ComplexionOptions => _coordinator.DataManager.ComplexionOptions;
        public ObservableCollection<string> TongueBodyOptions => _coordinator.DataManager.TongueBodyOptions;
        public ObservableCollection<string> TongueCoatingOptions => _coordinator.DataManager.TongueCoatingOptions;
        public ObservableCollection<string> PulseOptions => _coordinator.DataManager.PulseOptions;
        public ObservableCollection<string> SyndromeOptions => _coordinator.DataManager.SyndromeOptions;

        #endregion

        #region 命令（委托给协调器）

        public DelegateCommand SaveCommand => _coordinator.SaveCommand;
        public DelegateCommand ClearCommand => _coordinator.ClearCommand;
        public DelegateCommand<string> QuickInputCommand => _coordinator.QuickInputCommand;
        public DelegateCommand AnalyzeSymptomsCommand => _coordinator.AnalyzeSymptomsCommand;
        public DelegateCommand LoadDataCommand => _coordinator.LoadDataCommand;

        #endregion

        #region 构造函数

        public TCMFourDiagnosisViewModel(
            IConsultationApiService consultationApiService,
            ICustomDialogService dialogService,
            IMapper mapper,
            ITCMDiagnosisAnalyzer? diagnosisAnalyzer = null,
            ILogger<TCMFourDiagnosisCoordinator>? logger = null)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            
            // 创建协调器，委托所有操作
            _coordinator = new TCMFourDiagnosisCoordinator(
                consultationApiService, 
                dialogService, 
                diagnosisAnalyzer,
                logger);

            // 监听数据管理器的属性变化以保持UI同步
            _coordinator.DataManager.PropertyChanged += (s, e) => RaisePropertyChanged(e.PropertyName);
        }

        #endregion

        #region 公共方法（委托给协调器）

        /// <summary>
        /// 获取完整的望诊描述
        /// </summary>
        public string GetInspectionDescription() => 
            _coordinator.DescriptionGenerator.GetInspectionDescription(_coordinator.DataManager);

        /// <summary>
        /// 获取完整的闻诊描述
        /// </summary>
        public string GetAuscultationDescription() => 
            _coordinator.DescriptionGenerator.GetAuscultationDescription(_coordinator.DataManager);

        /// <summary>
        /// 获取完整的问诊描述
        /// </summary>
        public string GetInquiryDescription() => 
            _coordinator.DescriptionGenerator.GetInquiryDescription(_coordinator.DataManager);

        /// <summary>
        /// 获取完整的切诊描述
        /// </summary>
        public string GetPalpationDescription() => 
            _coordinator.DescriptionGenerator.GetPalpationDescription(_coordinator.DataManager);

        /// <summary>
        /// 映射到看诊更新信息模型
        /// UltraThink四层架构：先获取协调器DTO，然后转换为Info模型
        /// </summary>
        public ConsultationInfo MapToConsultationUpdateInfo()
        {
            var updateDto = _coordinator.MapToConsultationUpdateDto();
            return _mapper.Map<ConsultationInfo>(updateDto);
        }

        /// <summary>
        /// 从看诊详情信息中映射数据
        /// UltraThink四层架构：先将Info转换为DTO，然后传递给协调器
        /// </summary>
        public void MapFromConsultationDetail(ConsultationInfo consultationInfo)
        {
            var detailDto = _mapper.Map<ConsultationDetailDto>(consultationInfo);
            _coordinator.LoadFromConsultationDetail(detailDto);
        }

        /// <summary>
        /// 获取完整四诊数据
        /// </summary>
        public TCMFourDiagnosisData GetFourDiagnosisData() => 
            _coordinator.GetFourDiagnosisData();

        /// <summary>
        /// 应用快速输入模板
        /// </summary>
        public void ApplyQuickTemplate(string templateName) => 
            _coordinator.ApplyQuickTemplate(templateName);

        #endregion
    }

    /// <summary>
    /// 中医四诊数据结构（保持向后兼容）
    /// </summary>
    public class TCMFourDiagnosisData
    {
        public string Inspection { get; set; } = string.Empty;
        public string Auscultation { get; set; } = string.Empty;
        public string Inquiry { get; set; } = string.Empty;
        public string Palpation { get; set; } = string.Empty;
        public string TongueInspection { get; set; } = string.Empty;
        public string PulseCondition { get; set; } = string.Empty;
    }

    /// <summary>
    /// 中医诊断分析器接口（保持向后兼容）
    /// </summary>
    public interface ITCMDiagnosisAnalyzer
    {
        Task<List<string>> AnalyzeSyndromeAsync(TCMFourDiagnosisData data);
        Task<List<string>> RecommendTreatmentAsync(string syndrome);
    }

    /// <summary>
    /// 默认中医诊断分析器（保持向后兼容）
    /// </summary>
    public class DefaultTCMDiagnosisAnalyzer : ITCMDiagnosisAnalyzer
    {
        public async Task<List<string>> AnalyzeSyndromeAsync(TCMFourDiagnosisData data)
        {
            await Task.Delay(100); // 模拟分析过程

            var recommendations = new List<string>();

            // 简单的规则引擎示例
            if (data.Inspection.Contains("苍白") || data.PulseCondition.Contains("浮紧"))
            {
                recommendations.Add("风寒感冒");
            }

            if (data.Inquiry.Contains("发热") && data.TongueInspection.Contains("黄苔"))
            {
                recommendations.Add("风热感冒");
            }

            if (data.Inspection.Contains("萎黄") || data.PulseCondition.Contains("虚"))
            {
                recommendations.Add("脾胃虚弱");
            }

            if (data.TongueInspection.Contains("紫暗") || data.Inquiry.Contains("疼痛"))
            {
                recommendations.Add("血瘀证");
            }

            return recommendations;
        }

        public async Task<List<string>> RecommendTreatmentAsync(string syndrome)
        {
            await Task.Delay(50);

            return syndrome switch
            {
                "风寒感冒" => new List<string> { "辛温解表", "宣肺散寒" },
                "风热感冒" => new List<string> { "辛凉解表", "清热宣肺" },
                "脾胃虚弱" => new List<string> { "健脾益胃", "补中益气" },
                "血瘀证" => new List<string> { "活血化瘀", "理气止痛" },
                _ => new List<string> { "辨证论治" }
            };
        }
    }

    /// <summary>
    /// 中医模板数据结构（保持向后兼容）
    /// </summary>
    public class TCMTemplate
    {
        public string? ChiefComplaint { get; set; }
        public string? Complexion { get; set; }
        public string? Spirit { get; set; }
        public string? BodyShape { get; set; }
        public string? TongueBody { get; set; }
        public string? TongueCoating { get; set; }
        public string? Voice { get; set; }
        public string? Breath { get; set; }
        public string? Cough { get; set; }
        public string? ColdHeat { get; set; }
        public string? Sweat { get; set; }
        public string? HeadBody { get; set; }
        public string? ChestAbdomen { get; set; }
        public string? Appetite { get; set; }
        public string? Sleep { get; set; }
        public string? StoolUrine { get; set; }
        public string? LeftPulse { get; set; }
        public string? RightPulse { get; set; }
        public string? Syndrome { get; set; }
        public string? TreatmentPrinciple { get; set; }
    }

    /// <summary>
    /// 中医快速模板工具类（保持向后兼容）
    /// </summary>
    public static class TCMQuickTemplates
    {
        public static TCMTemplate? GetTemplate(string templateName)
        {
            var manager = new TCMFourDiagnosisTemplateManager();
            return manager.GetTemplate(templateName);
        }

        public static IEnumerable<string> GetAvailableTemplates()
        {
            var manager = new TCMFourDiagnosisTemplateManager();
            return manager.GetAvailableTemplates();
        }
    }
}