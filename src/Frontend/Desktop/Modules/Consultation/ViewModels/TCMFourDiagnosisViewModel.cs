using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.WPF.Client.Core.Interfaces.Services;

namespace LYBT.WPF.Client.Modules.Consultation.ViewModels
{
    /// <summary>
    /// 中医四诊视图模型 - 增强版（支持API集成和智能诊断）
    /// </summary>
    public class TCMFourDiagnosisViewModel : BindableBase
    {
        private readonly IConsultationApiService _consultationApiService;
        private readonly ICommonDialogService _dialogService;
        private readonly ITCMDiagnosisAnalyzer _diagnosisAnalyzer;

        private Guid _consultationId;
        public Guid ConsultationId
        {
            get => _consultationId;
            set => SetProperty(ref _consultationId, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private ObservableCollection<string> _recommendedSyndromes = new();
        public ObservableCollection<string> RecommendedSyndromes
        {
            get => _recommendedSyndromes;
            set => SetProperty(ref _recommendedSyndromes, value);
        }
        #region 望诊属性

        private string _complexion = ""; // 面色
        public string Complexion
        {
            get => _complexion;
            set => SetProperty(ref _complexion, value);
        }

        private string _spirit = ""; // 神态
        public string Spirit
        {
            get => _spirit;
            set => SetProperty(ref _spirit, value);
        }

        private string _bodyShape = ""; // 形态
        public string BodyShape
        {
            get => _bodyShape;
            set => SetProperty(ref _bodyShape, value);
        }

        private string _tongueBody = ""; // 舌质
        public string TongueBody
        {
            get => _tongueBody;
            set => SetProperty(ref _tongueBody, value);
        }

        private string _tongueCoating = ""; // 舌苔
        public string TongueCoating
        {
            get => _tongueCoating;
            set => SetProperty(ref _tongueCoating, value);
        }

        #endregion

        #region 闻诊属性

        private string _voice = ""; // 声音
        public string Voice
        {
            get => _voice;
            set => SetProperty(ref _voice, value);
        }

        private string _breath = ""; // 呼吸
        public string Breath
        {
            get => _breath;
            set => SetProperty(ref _breath, value);
        }

        private string _cough = ""; // 咳嗽
        public string Cough
        {
            get => _cough;
            set => SetProperty(ref _cough, value);
        }

        private string _odor = ""; // 气味
        public string Odor
        {
            get => _odor;
            set => SetProperty(ref _odor, value);
        }

        #endregion

        #region 问诊属性

        private string _chiefComplaint = ""; // 主诉
        public string ChiefComplaint
        {
            get => _chiefComplaint;
            set => SetProperty(ref _chiefComplaint, value);
        }

        private string _presentIllness = ""; // 现病史
        public string PresentIllness
        {
            get => _presentIllness;
            set => SetProperty(ref _presentIllness, value);
        }

        private string _coldHeat = ""; // 寒热
        public string ColdHeat
        {
            get => _coldHeat;
            set => SetProperty(ref _coldHeat, value);
        }

        private string _sweat = ""; // 汗
        public string Sweat
        {
            get => _sweat;
            set => SetProperty(ref _sweat, value);
        }

        private string _headBody = ""; // 头身
        public string HeadBody
        {
            get => _headBody;
            set => SetProperty(ref _headBody, value);
        }

        private string _chestAbdomen = ""; // 胸腹
        public string ChestAbdomen
        {
            get => _chestAbdomen;
            set => SetProperty(ref _chestAbdomen, value);
        }

        private string _appetite = ""; // 饮食
        public string Appetite
        {
            get => _appetite;
            set => SetProperty(ref _appetite, value);
        }

        private string _stoolUrine = ""; // 二便
        public string StoolUrine
        {
            get => _stoolUrine;
            set => SetProperty(ref _stoolUrine, value);
        }

        private string _sleep = ""; // 睡眠
        public string Sleep
        {
            get => _sleep;
            set => SetProperty(ref _sleep, value);
        }

        private string _menstruation = ""; // 月经（女性）
        public string Menstruation
        {
            get => _menstruation;
            set => SetProperty(ref _menstruation, value);
        }

        #endregion

        #region 切诊属性

        private string _pulseRate = ""; // 脉率
        public string PulseRate
        {
            get => _pulseRate;
            set => SetProperty(ref _pulseRate, value);
        }

        private string _pulseRhythm = ""; // 脉律
        public string PulseRhythm
        {
            get => _pulseRhythm;
            set => SetProperty(ref _pulseRhythm, value);
        }

        private string _pulseStrength = ""; // 脉力
        public string PulseStrength
        {
            get => _pulseStrength;
            set => SetProperty(ref _pulseStrength, value);
        }

        private string _pulseShape = ""; // 脉形
        public string PulseShape
        {
            get => _pulseShape;
            set => SetProperty(ref _pulseShape, value);
        }

        private string _leftPulse = ""; // 左脉
        public string LeftPulse
        {
            get => _leftPulse;
            set => SetProperty(ref _leftPulse, value);
        }

        private string _rightPulse = ""; // 右脉
        public string RightPulse
        {
            get => _rightPulse;
            set => SetProperty(ref _rightPulse, value);
        }

        private string _palpation = ""; // 按诊
        public string Palpation
        {
            get => _palpation;
            set => SetProperty(ref _palpation, value);
        }

        #endregion

        #region 综合诊断

        private string _tcmSyndrome = ""; // 中医证型
        public string TCMSyndrome
        {
            get => _tcmSyndrome;
            set => SetProperty(ref _tcmSyndrome, value);
        }

        private string _treatmentPrinciple = ""; // 治法
        public string TreatmentPrinciple
        {
            get => _treatmentPrinciple;
            set => SetProperty(ref _treatmentPrinciple, value);
        }

        #endregion

        #region 常用选项集合

        // 面色选项
        public ObservableCollection<string> ComplexionOptions { get; } = new ObservableCollection<string>
        {
            "红润", "苍白", "萎黄", "晦暗", "潮红", "青紫"
        };

        // 舌质选项
        public ObservableCollection<string> TongueBodyOptions { get; } = new ObservableCollection<string>
        {
            "淡红", "淡白", "红", "绛", "紫暗", "有瘀斑"
        };

        // 舌苔选项
        public ObservableCollection<string> TongueCoatingOptions { get; } = new ObservableCollection<string>
        {
            "薄白", "薄黄", "厚白", "厚黄", "白腻", "黄腻", "少苔", "无苔"
        };

        // 脉象选项
        public ObservableCollection<string> PulseOptions { get; } = new ObservableCollection<string>
        {
            "浮", "沉", "迟", "数", "虚", "实", "滑", "涩", "弦", "紧", "缓", "洪", "细", "弱"
        };

        // 常见证型
        public ObservableCollection<string> SyndromeOptions { get; } = new ObservableCollection<string>
        {
            "风寒感冒", "风热感冒", "暑湿感冒", "气虚感冒",
            "肝郁气滞", "肝火上炎", "肝阳上亢", "肝风内动",
            "脾胃虚寒", "脾胃湿热", "食积内停", "胃阴不足",
            "肺热咳嗽", "肺寒咳嗽", "燥邪犯肺", "痰湿蕴肺",
            "肾阳虚", "肾阴虚", "肾气不固", "肾精不足",
            "心血虚", "心阴虚", "心火亢盛", "心脾两虚",
            "气虚", "血虚", "阴虚", "阳虚", "气滞", "血瘀", "痰湿", "湿热"
        };

        #endregion

        #region 命令

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand ClearCommand { get; }
        public DelegateCommand<string> QuickInputCommand { get; }
        public DelegateCommand AnalyzeSymptomsCommand { get; }
        public DelegateCommand LoadDataCommand { get; }

        #endregion

        public TCMFourDiagnosisViewModel(
            IConsultationApiService consultationApiService,
            ICommonDialogService dialogService,
            ITCMDiagnosisAnalyzer? diagnosisAnalyzer = null)
        {
            _consultationApiService = consultationApiService;
            _dialogService = dialogService;
            _diagnosisAnalyzer = diagnosisAnalyzer ?? new DefaultTCMDiagnosisAnalyzer();

            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsLoading);
            ClearCommand = new DelegateCommand(Clear);
            QuickInputCommand = new DelegateCommand<string>(QuickInput);
            AnalyzeSymptomsCommand = new DelegateCommand(async () => await AnalyzeSymptomsAsync());
            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
        }

        #region 方法

        /// <summary>
        /// 获取完整的望诊描述
        /// </summary>
        public string GetInspectionDescription()
        {
            var parts = new[]
            {
                !string.IsNullOrWhiteSpace(Complexion) ? $"面色{Complexion}" : null,
                !string.IsNullOrWhiteSpace(Spirit) ? $"神{Spirit}" : null,
                !string.IsNullOrWhiteSpace(BodyShape) ? $"形态{BodyShape}" : null,
                !string.IsNullOrWhiteSpace(TongueBody) ? $"舌质{TongueBody}" : null,
                !string.IsNullOrWhiteSpace(TongueCoating) ? $"苔{TongueCoating}" : null
            };

            return string.Join("，", parts.Where(p => p != null));
        }

        /// <summary>
        /// 获取完整的闻诊描述
        /// </summary>
        public string GetAuscultationDescription()
        {
            var parts = new[]
            {
                !string.IsNullOrWhiteSpace(Voice) ? $"声音{Voice}" : null,
                !string.IsNullOrWhiteSpace(Breath) ? $"呼吸{Breath}" : null,
                !string.IsNullOrWhiteSpace(Cough) ? $"咳嗽{Cough}" : null,
                !string.IsNullOrWhiteSpace(Odor) ? $"气味{Odor}" : null
            };

            return string.Join("，", parts.Where(p => p != null));
        }

        /// <summary>
        /// 获取完整的问诊描述
        /// </summary>
        public string GetInquiryDescription()
        {
            var parts = new[]
            {
                !string.IsNullOrWhiteSpace(ChiefComplaint) ? $"主诉：{ChiefComplaint}" : null,
                !string.IsNullOrWhiteSpace(ColdHeat) ? $"寒热：{ColdHeat}" : null,
                !string.IsNullOrWhiteSpace(Sweat) ? $"汗：{Sweat}" : null,
                !string.IsNullOrWhiteSpace(HeadBody) ? $"头身：{HeadBody}" : null,
                !string.IsNullOrWhiteSpace(ChestAbdomen) ? $"胸腹：{ChestAbdomen}" : null,
                !string.IsNullOrWhiteSpace(Appetite) ? $"饮食：{Appetite}" : null,
                !string.IsNullOrWhiteSpace(StoolUrine) ? $"二便：{StoolUrine}" : null,
                !string.IsNullOrWhiteSpace(Sleep) ? $"睡眠：{Sleep}" : null,
                !string.IsNullOrWhiteSpace(Menstruation) ? $"月经：{Menstruation}" : null
            };

            return string.Join("；", parts.Where(p => p != null));
        }

        /// <summary>
        /// 获取完整的切诊描述
        /// </summary>
        public string GetPalpationDescription()
        {
            var parts = new[]
            {
                !string.IsNullOrWhiteSpace(LeftPulse) ? $"左脉{LeftPulse}" : null,
                !string.IsNullOrWhiteSpace(RightPulse) ? $"右脉{RightPulse}" : null,
                !string.IsNullOrWhiteSpace(PulseRate) ? $"脉率{PulseRate}" : null,
                !string.IsNullOrWhiteSpace(PulseRhythm) ? $"脉律{PulseRhythm}" : null,
                !string.IsNullOrWhiteSpace(PulseStrength) ? $"脉力{PulseStrength}" : null,
                !string.IsNullOrWhiteSpace(PulseShape) ? $"脉形{PulseShape}" : null,
                !string.IsNullOrWhiteSpace(Palpation) ? $"按诊：{Palpation}" : null
            };

            return string.Join("，", parts.Where(p => p != null));
        }

        private async Task SaveAsync()
        {
            if (ConsultationId == Guid.Empty)
            {
                await _dialogService.ShowWarningAsync("请先选择要更新的看诊记录", "警告");
                return;
            }

            try
            {
                IsLoading = true;

                var dto = MapToConsultationUpdateDto();
                var response = await _consultationApiService.UpdateConsultationAsync(ConsultationId, dto);

                if (response.IsSuccessStatusCode)
                {
                    await _dialogService.ShowInformationAsync("四诊信息保存成功", "成功");
                }
                else
                {
                    await _dialogService.ShowErrorAsync($"保存失败：{response.Error?.Content}", "错误");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"保存失败：{ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        private void Clear()
        {
            // 清空所有输入
            Complexion = "";
            Spirit = "";
            BodyShape = "";
            TongueBody = "";
            TongueCoating = "";
            
            Voice = "";
            Breath = "";
            Cough = "";
            Odor = "";
            
            ChiefComplaint = "";
            PresentIllness = "";
            ColdHeat = "";
            Sweat = "";
            HeadBody = "";
            ChestAbdomen = "";
            Appetite = "";
            StoolUrine = "";
            Sleep = "";
            Menstruation = "";
            
            PulseRate = "";
            PulseRhythm = "";
            PulseStrength = "";
            PulseShape = "";
            LeftPulse = "";
            RightPulse = "";
            Palpation = "";
            
            TCMSyndrome = "";
            TreatmentPrinciple = "";
        }

        private void QuickInput(string template)
        {
            if (string.IsNullOrWhiteSpace(template)) return;

            var templateData = TCMQuickTemplates.GetTemplate(template);
            if (templateData != null)
            {
                // 望诊
                Complexion = templateData.Complexion ?? Complexion;
                Spirit = templateData.Spirit ?? Spirit;
                TongueBody = templateData.TongueBody ?? TongueBody;
                TongueCoating = templateData.TongueCoating ?? TongueCoating;
                
                // 闻诊
                Voice = templateData.Voice ?? Voice;
                Breath = templateData.Breath ?? Breath;
                Cough = templateData.Cough ?? Cough;
                
                // 问诊
                ChiefComplaint = templateData.ChiefComplaint ?? ChiefComplaint;
                ColdHeat = templateData.ColdHeat ?? ColdHeat;
                Sweat = templateData.Sweat ?? Sweat;
                
                // 切诊
                LeftPulse = templateData.LeftPulse ?? LeftPulse;
                RightPulse = templateData.RightPulse ?? RightPulse;
                
                // 诊断
                TCMSyndrome = templateData.Syndrome ?? TCMSyndrome;
                TreatmentPrinciple = templateData.TreatmentPrinciple ?? TreatmentPrinciple;
            }
        }

        /// <summary>
        /// 加载看诊数据
        /// </summary>
        private async Task LoadDataAsync()
        {
            if (ConsultationId == Guid.Empty) return;

            try
            {
                IsLoading = true;

                var response = await _consultationApiService.GetByIdAsync(ConsultationId);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    MapFromConsultationDetail(response.Content);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"加载数据失败：{ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 映射到更新DTO
        /// </summary>
        private ConsultationUpdateDto MapToConsultationUpdateDto()
        {
            return new ConsultationUpdateDto
            {
                Inspection = GetInspectionDescription(),
                AuscultationOlfaction = GetAuscultationDescription(),
                Inquiry = GetInquiryDescription(),
                Palpation = GetPalpationDescription(),
                TongueInspection = $"舌质{TongueBody}，苔{TongueCoating}".Trim('，'),
                PulseCondition = $"左脉{LeftPulse}，右脉{RightPulse}".Trim('，'),
                TCMDiagnosis = TCMSyndrome,
                TreatmentPrinciple = TreatmentPrinciple,
                Remark = "由中医四诊系统生成"
            };
        }

        /// <summary>
        /// 从看诊详情中映射数据
        /// </summary>
        private void MapFromConsultationDetail(ConsultationDetailDto detail)
        {
            // 基础解析逻辑，实际项目中可能需要更复杂的解析
            if (!string.IsNullOrEmpty(detail.Inspection))
            {
                ParseInspectionData(detail.Inspection);
            }

            if (!string.IsNullOrEmpty(detail.AuscultationOlfaction))
            {
                ParseAuscultationData(detail.AuscultationOlfaction);
            }

            if (!string.IsNullOrEmpty(detail.Inquiry))
            {
                ParseInquiryData(detail.Inquiry);
            }

            if (!string.IsNullOrEmpty(detail.TongueInspection))
            {
                ParseTongueData(detail.TongueInspection);
            }

            if (!string.IsNullOrEmpty(detail.PulseCondition))
            {
                ParsePulseData(detail.PulseCondition);
            }

            TCMSyndrome = detail.TCMDiagnosis ?? "";
            TreatmentPrinciple = detail.TreatmentPrinciple ?? "";
        }

        /// <summary>
        /// 智能症状分析
        /// </summary>
        private async Task AnalyzeSymptomsAsync()
        {
            try
            {
                IsLoading = true;

                var currentData = new TCMFourDiagnosisData
                {
                    Inspection = GetInspectionDescription(),
                    Auscultation = GetAuscultationDescription(),
                    Inquiry = GetInquiryDescription(),
                    Palpation = GetPalpationDescription(),
                    TongueInspection = $"舌质{TongueBody}，苔{TongueCoating}",
                    PulseCondition = $"左脉{LeftPulse}，右脉{RightPulse}"
                };

                var recommendations = await _diagnosisAnalyzer.AnalyzeSyndromeAsync(currentData);
                
                RecommendedSyndromes.Clear();
                foreach (var syndrome in recommendations.Take(5))
                {
                    RecommendedSyndromes.Add(syndrome);
                }

                if (RecommendedSyndromes.Any())
                {
                    await _dialogService.ShowInformationAsync(
                        $"基于当前症状，推荐证型：{string.Join("、", RecommendedSyndromes)}", 
                        "智能诊断推荐");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"智能分析失败：{ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #region 数据解析辅助方法

        private void ParseInspectionData(string inspectionText)
        {
            // 简单的正则解析，实际可以使用更复杂的NLP
            if (inspectionText.Contains("面色"))
            {
                foreach (var option in ComplexionOptions)
                {
                    if (inspectionText.Contains(option))
                    {
                        Complexion = option;
                        break;
                    }
                }
            }
        }

        private void ParseAuscultationData(string auscultationText)
        {
            if (auscultationText.Contains("声音"))
            {
                Voice = auscultationText;
            }
        }

        private void ParseInquiryData(string inquiryText)
        {
            if (inquiryText.Contains("主诉"))
            {
                var parts = inquiryText.Split('；');
                foreach (var part in parts)
                {
                    if (part.Contains("主诉"))
                    {
                        ChiefComplaint = part.Replace("主诉：", "").Trim();
                    }
                }
            }
        }

        private void ParseTongueData(string tongueText)
        {
            foreach (var bodyOption in TongueBodyOptions)
            {
                if (tongueText.Contains(bodyOption))
                {
                    TongueBody = bodyOption;
                    break;
                }
            }

            foreach (var coatingOption in TongueCoatingOptions)
            {
                if (tongueText.Contains(coatingOption))
                {
                    TongueCoating = coatingOption;
                    break;
                }
            }
        }

        private void ParsePulseData(string pulseText)
        {
            if (pulseText.Contains("左脉"))
            {
                var leftPart = pulseText.Substring(pulseText.IndexOf("左脉") + 2);
                if (leftPart.Contains("、") || leftPart.Contains("，"))
                {
                    LeftPulse = leftPart.Split('、', '，')[0].Trim();
                }
            }

            if (pulseText.Contains("右脉"))
            {
                var rightPart = pulseText.Substring(pulseText.IndexOf("右脉") + 2);
                if (rightPart.Contains("、") || rightPart.Contains("，"))
                {
                    RightPulse = rightPart.Split('、', '，')[0].Trim();
                }
            }
        }

        #endregion

        #endregion
    }

    #region 支持类和接口

    /// <summary>
    /// 中医四诊数据结构
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
    /// 中医诊断分析器接口
    /// </summary>
    public interface ITCMDiagnosisAnalyzer
    {
        Task<List<string>> AnalyzeSyndromeAsync(TCMFourDiagnosisData data);
        Task<List<string>> RecommendTreatmentAsync(string syndrome);
    }

    /// <summary>
    /// 默认中医诊断分析器
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
    /// 中医快速模板工具类
    /// </summary>
    public static class TCMQuickTemplates
    {
        private static readonly Dictionary<string, TCMTemplate> _templates = new()
        {
            ["风寒感冒"] = new TCMTemplate
            {
                ChiefComplaint = "恶寒重，发热轻，头痛，鼻塞，流清涕",
                Complexion = "苍白",
                Spirit = "精神疲惫",
                Voice = "声音低微",
                ColdHeat = "恶寒重，发热轻",
                TongueBody = "淡红",
                TongueCoating = "苔薄白",
                LeftPulse = "浮紧",
                RightPulse = "浮紧",
                Syndrome = "风寒束表证",
                TreatmentPrinciple = "辛温解表，宣肺散寒"
            },
            ["风热感冒"] = new TCMTemplate
            {
                ChiefComplaint = "发热重，恶寒轻，头痛，咽痛，口渴",
                Complexion = "面色潮红",
                Voice = "声音咳嗄",
                ColdHeat = "发热重，恶寒轻",
                TongueBody = "红",
                TongueCoating = "苔薄黄",
                LeftPulse = "浮数",
                RightPulse = "浮数",
                Syndrome = "风热犯表证",
                TreatmentPrinciple = "辛凉解表，清热宣肺"
            }
        };

        public static TCMTemplate? GetTemplate(string templateName)
        {
            return _templates.TryGetValue(templateName, out var template) ? template : null;
        }

        public static IEnumerable<string> GetAvailableTemplates()
        {
            return _templates.Keys;
        }
    }

    /// <summary>
    /// 中医模板数据结构
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
        public string? LeftPulse { get; set; }
        public string? RightPulse { get; set; }
        public string? Syndrome { get; set; }
        public string? TreatmentPrinciple { get; set; }
    }

    #endregion
}