using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Events;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Consultation.ViewModels;

using LYBT.Desktop.Core.Models.Consultation;
namespace LYBT.Desktop.Consultation.ViewModels
{
    /// <summary>
    /// 辨证分析视图模型
    /// 用于输入中医证型诊断和治法治则
    /// </summary>
    public class DifferentiationViewModel : BindableBase
    {
        #region 依赖服务

        private readonly IEventAggregator _eventAggregator;
        private readonly IConsultationService _consultationService;
        private readonly ICustomDialogService _dialogService;
        private readonly ILogger<DifferentiationViewModel> _logger;

        #endregion

        #region 属性

        private Guid _medicalCaseId;
        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        // 中医证型
        private string _tcmSyndrome = "";
        public string TCMSyndrome
        {
            get => _tcmSyndrome;
            set
            {
                if (SetProperty(ref _tcmSyndrome, value))
                {
                    OnDataChanged();
                    UpdateSyndromeButtons();
                }
            }
        }

        // 辨证分析过程
        private string _differentiationAnalysis = "";
        public string DifferentiationAnalysis
        {
            get => _differentiationAnalysis;
            set
            {
                if (SetProperty(ref _differentiationAnalysis, value))
                {
                    OnDataChanged();
                }
            }
        }

        // 治法治则
        private string _treatmentPrinciple = "";
        public string TreatmentPrinciple
        {
            get => _treatmentPrinciple;
            set
            {
                if (SetProperty(ref _treatmentPrinciple, value))
                {
                    OnDataChanged();
                }
            }
        }

        // 西医诊断（可选）
        private string _westernDiagnosis = "";
        public string WesternDiagnosis
        {
            get => _westernDiagnosis;
            set
            {
                if (SetProperty(ref _westernDiagnosis, value))
                {
                    OnDataChanged();
                }
            }
        }

        // 备注
        private string _remark = "";
        public string Remark
        {
            get => _remark;
            set
            {
                if (SetProperty(ref _remark, value))
                {
                    OnDataChanged();
                }
            }
        }

        private string _importSource = "";
        public string ImportSource
        {
            get => _importSource;
            set => SetProperty(ref _importSource, value);
        }

        private bool _hasImportedData;
        public bool HasImportedData
        {
            get => _hasImportedData;
            set => SetProperty(ref _hasImportedData, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _hasChanges;
        public bool HasChanges
        {
            get => _hasChanges;
            set => SetProperty(ref _hasChanges, value);
        }

        // 输入提示
        public string TCMSyndromeHint => "请输入中医证型，如：风寒感冒、肝郁气滞、脾胃虚寒等...";
        public string DifferentiationHint => "请输入辨证分析过程，包括病因病机、证候特点等...";
        public string TreatmentPrincipleHint => "请输入治法治则，如：疏肝解郁、健脾益气、活血化瘀等...";
        public string WesternDiagnosisHint => "（可选）输入西医诊断参考...";
        public string RemarkHint => "（可选）补充说明或注意事项...";

        #endregion

        #region 常用证型集合

        public ObservableCollection<SyndromeOption> CommonSyndromes { get; } = new ObservableCollection<SyndromeOption>
        {
            // 感冒类
            new SyndromeOption { Name = "风寒感冒", Category = "外感", TreatmentPrinciple = "辛温解表，宣肺散寒" },
            new SyndromeOption { Name = "风热感冒", Category = "外感", TreatmentPrinciple = "辛凉解表，清热宣肺" },
            new SyndromeOption { Name = "暑湿感冒", Category = "外感", TreatmentPrinciple = "清暑祛湿，解表和中" },
            
            // 肝系
            new SyndromeOption { Name = "肝郁气滞", Category = "肝系", TreatmentPrinciple = "疏肝解郁，理气和中" },
            new SyndromeOption { Name = "肝火上炎", Category = "肝系", TreatmentPrinciple = "清肝泻火，平肝潜阳" },
            new SyndromeOption { Name = "肝阳上亢", Category = "肝系", TreatmentPrinciple = "平肝潜阳，滋阴降火" },
            
            // 脾胃系
            new SyndromeOption { Name = "脾胃虚寒", Category = "脾胃", TreatmentPrinciple = "温中健脾，和胃止痛" },
            new SyndromeOption { Name = "脾胃湿热", Category = "脾胃", TreatmentPrinciple = "清热化湿，和胃降逆" },
            new SyndromeOption { Name = "食积内停", Category = "脾胃", TreatmentPrinciple = "消食导滞，和胃降逆" },
            
            // 肺系
            new SyndromeOption { Name = "肺热咳嗽", Category = "肺系", TreatmentPrinciple = "清热宣肺，止咳化痰" },
            new SyndromeOption { Name = "肺寒咳嗽", Category = "肺系", TreatmentPrinciple = "温肺散寒，止咳化痰" },
            new SyndromeOption { Name = "痰湿蕴肺", Category = "肺系", TreatmentPrinciple = "燥湿化痰，理气和中" },
            
            // 肾系
            new SyndromeOption { Name = "肾阳虚", Category = "肾系", TreatmentPrinciple = "温补肾阳，益火之源" },
            new SyndromeOption { Name = "肾阴虚", Category = "肾系", TreatmentPrinciple = "滋补肾阴，壮水之主" },
            new SyndromeOption { Name = "肾气不固", Category = "肾系", TreatmentPrinciple = "补肾固摄，益气养阴" },
            
            // 心系
            new SyndromeOption { Name = "心血虚", Category = "心系", TreatmentPrinciple = "补血养心，安神定志" },
            new SyndromeOption { Name = "心火亢盛", Category = "心系", TreatmentPrinciple = "清心泻火，安神定志" },
            new SyndromeOption { Name = "心脾两虚", Category = "心系", TreatmentPrinciple = "补益心脾，养血安神" },
            
            // 基本证型
            new SyndromeOption { Name = "气虚证", Category = "基本证型", TreatmentPrinciple = "补气健脾" },
            new SyndromeOption { Name = "血虚证", Category = "基本证型", TreatmentPrinciple = "补血养血" },
            new SyndromeOption { Name = "阴虚证", Category = "基本证型", TreatmentPrinciple = "滋阴降火" },
            new SyndromeOption { Name = "阳虚证", Category = "基本证型", TreatmentPrinciple = "温补阳气" },
            new SyndromeOption { Name = "气滞证", Category = "基本证型", TreatmentPrinciple = "理气解郁" },
            new SyndromeOption { Name = "血瘀证", Category = "基本证型", TreatmentPrinciple = "活血化瘀" },
            new SyndromeOption { Name = "痰湿证", Category = "基本证型", TreatmentPrinciple = "化痰祛湿" },
            new SyndromeOption { Name = "湿热证", Category = "基本证型", TreatmentPrinciple = "清热化湿" }
        };

        // 常用治法
        public ObservableCollection<string> CommonTreatmentPrinciples { get; } = new ObservableCollection<string>
        {
            "疏肝解郁", "健脾益气", "补血养心", "滋阴降火",
            "温补肾阳", "活血化瘀", "化痰祛湿", "清热解毒",
            "宣肺止咳", "理气和中", "养阴生津", "平肝潜阳",
            "补益肝肾", "安神定志", "消食导滞", "温中散寒"
        };

        #endregion

        #region 命令

        public ICommand SaveCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SelectSyndromeCommand { get; }
        public ICommand AddTreatmentPrincipleCommand { get; }
        public ICommand ImportFromHistoryCommand { get; }
        public ICommand AnalyzeFromFourDiagnosisCommand { get; }

        #endregion

        #region 构造函数

        public DifferentiationViewModel(
            IEventAggregator eventAggregator,
            IConsultationService consultationService,
            ICustomDialogService dialogService,
            ILogger<DifferentiationViewModel> logger)
        {
            _eventAggregator = eventAggregator;
            _consultationService = consultationService;
            _dialogService = dialogService;
            _logger = logger;

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsLoading && HasChanges);
            ClearCommand = new DelegateCommand(Clear, () => !IsLoading);
            SelectSyndromeCommand = new DelegateCommand<SyndromeOption>(SelectSyndrome);
            AddTreatmentPrincipleCommand = new DelegateCommand<string>(AddTreatmentPrinciple);
            ImportFromHistoryCommand = new DelegateCommand(async () => await ImportFromHistoryAsync());
            AnalyzeFromFourDiagnosisCommand = new DelegateCommand(async () => await AnalyzeFromFourDiagnosisAsync());

            // 订阅事件
            SubscribeEvents();
        }

        #endregion

        #region 初始化

        private void SubscribeEvents()
        {
            // 订阅保存步骤数据事件
            _eventAggregator.GetEvent<SaveStepDataEvent>().Subscribe(OnSaveStepData);
            
            // 订阅四诊数据保存事件，用于智能分析
            _eventAggregator.GetEvent<FourDiagnosisSavedEvent>().Subscribe(OnFourDiagnosisSaved);
        }

        public async Task InitializeAsync(Guid medicalCaseId)
        {
            try
            {
                IsLoading = true;
                MedicalCaseId = medicalCaseId;

                // 加载已有数据
                await LoadExistingDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化辨证分析失败");
                await _dialogService.ShowErrorAsync("初始化失败: " + ex.Message, "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadExistingDataAsync()
        {
            if (MedicalCaseId == Guid.Empty) return;

            try
            {
                var result = await _consultationService.GetByMedicalCaseIdAsync(MedicalCaseId);
                if (result.IsSuccess && result.Data != null)
                {
                    var data = result.Data;
                    TCMSyndrome = data.TCMDiagnosis ?? "";
                    DifferentiationAnalysis = data.DifferentiationAnalysis ?? "";
                    TreatmentPrinciple = data.TreatmentPrinciple ?? "";
                    WesternDiagnosis = data.Diagnosis ?? "";
                    Remark = data.Remark ?? "";
                    
                    // 重置更改标记
                    HasChanges = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载辨证数据失败");
            }
        }

        #endregion

        #region 数据操作

        private async Task SaveAsync()
        {
            try
            {
                IsLoading = true;

                // 构建诊断字符串
                var diagnosis = string.IsNullOrWhiteSpace(WesternDiagnosis)
                    ? TCMSyndrome
                    : $"{TCMSyndrome}（西医诊断：{WesternDiagnosis}）";

                // 发布步骤完成事件
                var stepData = new WorkflowStepData
                {
                    Step = WorkflowStep.Differentiation,
                    Data = diagnosis
                };
                _eventAggregator.GetEvent<WorkflowStepCompletedEvent>().Publish(stepData);

                // 发布诊断保存事件
                var diagnosisData = new DiagnosisSavedEventArgs
                {
                    Diagnosis = TCMSyndrome,
                    DifferentiationAnalysis = DifferentiationAnalysis,
                    DiagnosisTime = DateTime.Now
                };
                _eventAggregator.GetEvent<DiagnosisSavedEvent>().Publish(diagnosisData);

                HasChanges = false;
                await _dialogService.ShowInformationAsync("辨证分析保存成功", "成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存辨证分析失败");
                await _dialogService.ShowErrorAsync("保存失败: " + ex.Message, "错误");
            }
            finally
            {
                IsLoading = false;
                (SaveCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            }
        }

        private void Clear()
        {
            var confirm = _dialogService.ShowConfirmationAsync(
                "确定要清空所有辨证分析内容吗？",
                "清空确认").Result;
                
            if (confirm)
            {
                TCMSyndrome = "";
                DifferentiationAnalysis = "";
                TreatmentPrinciple = "";
                WesternDiagnosis = "";
                Remark = "";
                ImportSource = "";
                HasImportedData = false;
                HasChanges = true;
            }
        }

        private void SelectSyndrome(SyndromeOption? syndrome)
        {
            if (syndrome == null) return;

            // 如果已有内容，询问是否替换
            if (!string.IsNullOrWhiteSpace(TCMSyndrome))
            {
                var replace = _dialogService.ShowConfirmationAsync(
                    $"是否用\"{syndrome.Name}\"替换当前证型？",
                    "替换确认").Result;
                    
                if (!replace)
                {
                    // 追加模式
                    TCMSyndrome += $"、{syndrome.Name}";
                    if (!string.IsNullOrWhiteSpace(syndrome.TreatmentPrinciple))
                    {
                        if (!string.IsNullOrWhiteSpace(TreatmentPrinciple))
                            TreatmentPrinciple += "，";
                        TreatmentPrinciple += syndrome.TreatmentPrinciple;
                    }
                    return;
                }
            }

            // 替换模式
            TCMSyndrome = syndrome.Name;
            TreatmentPrinciple = syndrome.TreatmentPrinciple ?? "";
            HasChanges = true;
        }

        private void AddTreatmentPrinciple(string? principle)
        {
            if (string.IsNullOrWhiteSpace(principle)) return;

            if (!string.IsNullOrWhiteSpace(TreatmentPrinciple))
            {
                TreatmentPrinciple += "，";
            }
            TreatmentPrinciple += principle;
            HasChanges = true;
        }

        private async Task ImportFromHistoryAsync()
        {
            try
            {
                // TODO: 实现历史记录选择对话框
                await _dialogService.ShowInformationAsync(
                    "从历史记录导入功能将从患者历史就诊中选择辨证信息",
                    "功能提示");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入历史辨证失败");
                await _dialogService.ShowErrorAsync("导入失败: " + ex.Message, "错误");
            }
        }

        private async Task AnalyzeFromFourDiagnosisAsync()
        {
            try
            {
                IsLoading = true;

                // 获取四诊数据进行智能分析
                var fourDiagnosisResult = await _consultationService.GetFourDiagnosisByMedicalCaseIdAsync(MedicalCaseId);
                if (fourDiagnosisResult.IsSuccess && fourDiagnosisResult.Data != null)
                {
                    var data = fourDiagnosisResult.Data;
                    
                    // 基于规则的简单分析
                    var suggestedSyndrome = AnalyzeSyndrome(data);
                    if (!string.IsNullOrWhiteSpace(suggestedSyndrome))
                    {
                        TCMSyndrome = suggestedSyndrome;
                        
                        // 查找对应的治法
                        var syndrome = CommonSyndromes.FirstOrDefault(s => s.Name == suggestedSyndrome);
                        if (syndrome != null)
                        {
                            TreatmentPrinciple = syndrome.TreatmentPrinciple ?? "";
                        }
                        
                        HasChanges = true;
                        await _dialogService.ShowInformationAsync(
                            $"基于四诊信息，建议证型为：{suggestedSyndrome}",
                            "智能分析");
                    }
                    else
                    {
                        await _dialogService.ShowInformationAsync(
                            "未能从四诊信息中分析出明确证型，请手动输入",
                            "分析结果");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "智能分析失败");
                await _dialogService.ShowErrorAsync("分析失败: " + ex.Message, "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string AnalyzeSyndrome(FourDiagnosisData data)
        {
            // 简单的规则匹配（实际应该使用更复杂的分析逻辑）
            var text = $"{data.Inspection} {data.Auscultation} {data.Inquiry} {data.Palpation}".ToLower();

            if (text.Contains("恶寒") && text.Contains("发热") && text.Contains("无汗"))
                return "风寒感冒";
            
            if (text.Contains("发热") && text.Contains("咽痛") && text.Contains("口渴"))
                return "风热感冒";
            
            if (text.Contains("胁痛") || text.Contains("情志") || text.Contains("抑郁"))
                return "肝郁气滞";
            
            if (text.Contains("胃痛") && text.Contains("喜温") && text.Contains("喜按"))
                return "脾胃虚寒";
            
            if (text.Contains("咳嗽") && text.Contains("痰黄") && text.Contains("口干"))
                return "肺热咳嗽";
            
            if (text.Contains("腰膝酸软") && text.Contains("畏寒") && text.Contains("肢冷"))
                return "肾阳虚";

            return "";
        }

        #endregion

        #region 事件处理

        private void OnDataChanged()
        {
            HasChanges = true;
            (SaveCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }

        private void UpdateSyndromeButtons()
        {
            // 更新常用证型按钮的选中状态
            foreach (var syndrome in CommonSyndromes)
            {
                syndrome.IsSelected = TCMSyndrome.Contains(syndrome.Name);
            }
        }

        private void OnSaveStepData(WorkflowStep step)
        {
            if (step == WorkflowStep.Differentiation)
            {
                // 自动保存当前数据
                _ = SaveAsync();
            }
        }

        private void OnFourDiagnosisSaved(LYBT.Desktop.Core.Events.TCMFourDiagnosisData data)
        {
            // 当四诊数据保存时，可以提示进行智能分析
            if (string.IsNullOrWhiteSpace(TCMSyndrome))
            {
                _ = _dialogService.ShowInformationAsync(
                    "四诊信息已更新，可以点击\"智能分析\"按钮进行辨证分析",
                    "提示");
            }
        }

        #endregion

        #region 数据导出

        /// <summary>
        /// 获取辨证数据用于工作流
        /// </summary>
        public string GetDiagnosisData()
        {
            return TCMSyndrome;
        }

        /// <summary>
        /// 获取完整的辨证分析数据
        /// </summary>
        public DifferentiationData GetFullData()
        {
            return new DifferentiationData
            {
                TCMSyndrome = TCMSyndrome,
                DifferentiationAnalysis = DifferentiationAnalysis,
                TreatmentPrinciple = TreatmentPrinciple,
                WesternDiagnosis = WesternDiagnosis,
                Remark = Remark
            };
        }

        #endregion

        #region 内部类型

        /// <summary>
        /// 证型选项
        /// </summary>
        public class SyndromeOption : BindableBase
        {
            public string Name { get; set; } = "";
            public string Category { get; set; } = "";
            public string? TreatmentPrinciple { get; set; }
            
            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set => SetProperty(ref _isSelected, value);
            }
        }

        /// <summary>
        /// 辨证数据
        /// </summary>
        public class DifferentiationData
        {
            public string TCMSyndrome { get; set; } = "";
            public string DifferentiationAnalysis { get; set; } = "";
            public string TreatmentPrinciple { get; set; } = "";
            public string WesternDiagnosis { get; set; } = "";
            public string Remark { get; set; } = "";
        }

        #endregion
    }
}