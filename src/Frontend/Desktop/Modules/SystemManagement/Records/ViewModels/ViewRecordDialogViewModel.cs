using System;
using System.Linq;
using System.Windows;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Records;
using Prism.Commands;
using Prism.Mvvm;

using Prism.Dialogs;
namespace LYBT.WPF.Client.Modules.SystemManagement.Records.ViewModels
{
    /// <summary>
    /// 查看病历详情对话框视图模型
    /// </summary>
    public class ViewRecordDialogViewModel : BindableBase
    {
        private string _title = "详情";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }


        private readonly ICommonDialogService _commonDialogService;

        private readonly IRecordService _recordService;

        #region 属性

        private RecordDetailDto? _record;
        private bool _isLoading = true;

        /// <summary>病历详情</summary>
        public RecordDetailDto? Record
        {
            get => _record;
            set => SetProperty(ref _record, value);
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region 计算属性

        /// <summary>诊断结果文本</summary>
        public string DiagnosisResultsText => Record != null && Record.DiagnosisResults.Count > 0 
            ? string.Join("、", Record.DiagnosisResults) 
            : "无";

        /// <summary>共享状态文本</summary>
        public string ShareStatusText => Record?.IsShared == true 
            ? $"已共享给 {Record.SharedToDoctorIds.Count} 位医生" 
            : "未共享";

        /// <summary>处方状态文本</summary>
        public string PrescriptionStatusText => Record?.PrescriptionId.HasValue == true 
            ? "已开处方" 
            : "未开处方";

        /// <summary>验方模板文本</summary>
        public string FormulaTemplateText => Record?.FormulaTemplateId.HasValue == true 
            ? "已使用验方模板" 
            : "未使用验方模板";

        /// <summary>药材组成文本</summary>
        public string HerbalFormulaText
        {
            get
            {
                if (Record?.HerbalFormula == null || Record.HerbalFormula.Count == 0)
                    return "无";

                var herbsInfo = Record.HerbalFormula.Select(h => $"{h.Name} {h.Dosage}{h.Unit}");
                return string.Join("，", herbsInfo);
            }
        }

        /// <summary>治疗方案文本</summary>
        public string TreatmentPlansText
        {
            get
            {
                if (Record?.TreatmentPlans == null || Record.TreatmentPlans.Count == 0)
                    return "无";

                return $"共 {Record.TreatmentPlans.Count} 项治疗方案";
            }
        }

        #endregion

        #region 命令

        public DelegateCommand CloseCommand { get; }
        public DelegateCommand PrintCommand { get; }
        public DelegateCommand ExportCommand { get; }

        #endregion

        public ViewRecordDialogViewModel(IRecordService recordService,
            ICommonDialogService commonDialogService)
        {
            Title = "病历详情";
            _commonDialogService = commonDialogService;
            _recordService = recordService;
            // _recordId = recordId; // Variable not needed

            CloseCommand = new DelegateCommand(ExecuteClose);
            PrintCommand = new DelegateCommand(ExecutePrint);
            ExportCommand = new DelegateCommand(ExecuteExport);

            // 加载病历详情
            // _ = LoadRecordAsync(); // TODO: Pass record ID
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("recordId"))
            {
                var id = parameters.GetValue<Guid>("recordId");
                _ = LoadRecordAsync(id);
            }

        }

        private async System.Threading.Tasks.Task LoadRecordAsync(Guid id)
        {
            try
            {
                IsLoading = true;
                var result = await _recordService.GetByIdAsync(id);
                
                if (result.IsSuccess && result.Data != null)
                {
                    Record = result.Data;
                    
                    // 触发计算属性更新
                    RaisePropertyChanged(nameof(DiagnosisResultsText));
                    RaisePropertyChanged(nameof(ShareStatusText));
                    RaisePropertyChanged(nameof(PrescriptionStatusText));
                    RaisePropertyChanged(nameof(FormulaTemplateText));
                    RaisePropertyChanged(nameof(HerbalFormulaText));
                    RaisePropertyChanged(nameof(TreatmentPlansText));
                }
                else
                {
                    _commonDialogService.ShowErrorAsync($"加载病历详情失败：{result.ErrorMessage}", "错误").GetAwaiter().GetResult();
                    RaiseRequestClose(new DialogResult(ButtonResult.OK));
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"加载病历详情失败：{ex.Message}", "错误").GetAwaiter().GetResult();
                RaiseRequestClose(new DialogResult(ButtonResult.OK));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteClose()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.OK));
        }

        private void ExecutePrint()
        {
            // TODO: 实现打印功能
            _commonDialogService.ShowInformationAsync("病历打印功能开发中...", "提示").GetAwaiter().GetResult();
        }

        private void ExecuteExport()
        {
            // TODO: 实现导出功能
            _commonDialogService.ShowInformationAsync("病历导出功能开发中...", "提示").GetAwaiter().GetResult();
        }
        // 临时占位方法 - 等待IDialogAware问题解决
        private void RaiseRequestClose(IDialogResult dialogResult)
        {
            // TODO: 实现对话框关闭逻辑
        }



        /* #region IDialogAware Implementation

        event Action<IDialogResult> IDialogAware.RequestClose
        {
            add { _requestClose += value; }
            remove { _requestClose -= value; }
        }
        
        private Action<IDialogResult>? _requestClose;

        private void RaiseRequestClose(IDialogResult dialogResult)
        {
            _requestClose?.Invoke(dialogResult);
        }

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        #endregion */
        }
}