using System;
using System.Collections.ObjectModel;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.FormulaTemplates;

using Prism.Dialogs;
namespace LYBT.WPF.Client.Modules.SystemManagement.FormulaTemplates.ViewModels
{
    /// <summary>
    /// 查看验方模板对话框视图模型
    /// </summary>
    public class ViewFormulaTemplateDialogViewModel : BindableBase
    {
        private string _title = "详情";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }


        private readonly ICommonDialogService _commonDialogService;

        private readonly IFormulaTemplateService _formulaTemplateService;
        private readonly Window _window;
        private Guid _templateId;

        #region Properties

        private string _templateName = string.Empty;
        public string TemplateName
        {
            get => _templateName;
            set => SetProperty(ref _templateName, value);
        }

        private string _category = string.Empty;
        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        private string _indications = string.Empty;
        public string Indications
        {
            get => _indications;
            set => SetProperty(ref _indications, value);
        }

        private string _efficacy = string.Empty;
        public string Efficacy
        {
            get => _efficacy;
            set => SetProperty(ref _efficacy, value);
        }

        private string _usage = string.Empty;
        public string Usage
        {
            get => _usage;
            set => SetProperty(ref _usage, value);
        }

        private string _remark = string.Empty;
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        private DateTime _createTime;
        public DateTime CreateTime
        {
            get => _createTime;
            set => SetProperty(ref _createTime, value);
        }

        private ObservableCollection<FormulaTemplateHerbItem> _templateHerbs = new();
        public ObservableCollection<FormulaTemplateHerbItem> TemplateHerbs
        {
            get => _templateHerbs;
            set => SetProperty(ref _templateHerbs, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commands

        public DelegateCommand CopyCommand { get; }
        public DelegateCommand CloseCommand { get; }

        #endregion

        public ViewFormulaTemplateDialogViewModel(IFormulaTemplateService formulaTemplateService,
            ICommonDialogService commonDialogService)
        {
            Title = "验方模板详情";
            _commonDialogService = commonDialogService;
            _formulaTemplateService = formulaTemplateService;

            CopyCommand = new DelegateCommand(ExecuteCopy);
            CloseCommand = new DelegateCommand(ExecuteClose);

            // 获取当前窗口实例
            _window = Application.Current.Windows[Application.Current.Windows.Count - 1];
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            // 支持两种参数名称，确保兼容性
            if (parameters.ContainsKey("formulaTemplateId"))
            {
                var id = parameters.GetValue<Guid>("formulaTemplateId");
                _ = LoadFormulaTemplateAsync(id);
            }
            else if (parameters.ContainsKey("templateId"))
            {
                var id = parameters.GetValue<Guid>("templateId");
                _ = LoadFormulaTemplateAsync(id);
            }
        }
        
        private async System.Threading.Tasks.Task LoadFormulaTemplateAsync(Guid templateId)
        {
            try
            {
                IsLoading = true;
                _templateId = templateId;
                await LoadTemplateData();
            }
            catch (Exception)
            {
                // Handle error
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async void Initialize(Guid templateId)
        {
            _templateId = templateId;
            await LoadTemplateData();
        }

        private async System.Threading.Tasks.Task LoadTemplateData()
        {
            try
            {
                var response = await _formulaTemplateService.GetByIdAsync(_templateId);
                if (response.IsSuccess && response.Data != null)
                {
                    var template = response.Data;
                    TemplateName = template.Name;
                    Category = template.Category;
                    Indications = template.Indications ?? "无";
                    Efficacy = template.Efficacy ?? "无";
                    Usage = template.Usage ?? "无";
                    Remark = template.Remark ?? "无";
                    CreateTime = template.CreateTime;

                    // 加载药材列表
                    TemplateHerbs.Clear();
                    if (template.Herbs != null)
                    {
                        foreach (var herb in template.Herbs)
                        {
                            TemplateHerbs.Add(new FormulaTemplateHerbItem
                            {
                                HerbId = herb.HerbId,
                                HerbName = herb.HerbName,
                                Quantity = herb.Quantity,
                                Unit = herb.Unit,
                                Remark = herb.Remark,
                                SortOrder = herb.SortOrder
                            });
                        }
                    }
                }
                else
                {
                    _commonDialogService.ShowErrorAsync($"加载验方模板失败: {response.ErrorMessage}", "错误").GetAwaiter().GetResult();
                    _window.Close();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"加载验方模板失败: {ex.Message}", "错误").GetAwaiter().GetResult();
                _window.Close();
            }
        }

        private void ExecuteCopy()
        {
            try
            {
                // TODO: 实现复制功能
                _commonDialogService.ShowInformationAsync("验方模板复制功能待实现", "提示").GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"复制验方模板失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private void ExecuteClose()
        {
            _window.Close();
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