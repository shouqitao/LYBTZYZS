using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
// UltraThink v2.0: 直接使用DTOs，移除Info模型引用
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Models.Common;
using Prism.Commands;
using Prism.Events;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 编辑验方对话框视图模型
    /// </summary>
    public class EditFormulaDialogViewModel : DialogViewModel, ICustomDialogAware
    {
        private readonly ICustomDialogService _dialogService;
        private readonly IFormulaService _formulaService;
        private readonly IHerbService _herbService;
        private readonly ILogger<EditFormulaDialogViewModel> _logger;
        private Guid _formulaId;

        #region Properties

        private FormulaDto _formula = new();
        public FormulaDto Formula
        {
            get => _formula;
            set => SetProperty(ref _formula, value);
        }

        private ObservableCollection<FormulaHerbItemDto> _herbItems = new();
        public ObservableCollection<FormulaHerbItemDto> HerbItems
        {
            get => _herbItems;
            set => SetProperty(ref _herbItems, value);
        }

        private FormulaHerbItemDto? _selectedHerbItem;
        public FormulaHerbItemDto? SelectedHerbItem
        {
            get => _selectedHerbItem;
            set => SetProperty(ref _selectedHerbItem, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<string> Categories { get; } = new()
        {
            "内科方", "外科方", "妇科方", "儿科方",
            "皮肤科方", "五官科方", "骨伤科方", "经典方",
            "时方", "验方", "其他"
        };

        public ObservableCollection<HerbDto> AvailableHerbs { get; } = new();

        #endregion

        #region Commands

        public DelegateCommand SaveCommand { get; } = null!;
        public DelegateCommand CancelCommand { get; } = null!;
        public DelegateCommand AddHerbCommand { get; } = null!;
        public DelegateCommand<FormulaHerbItemDto> RemoveHerbCommand { get; } = null!;
        public DelegateCommand<FormulaHerbItemDto> EditHerbCommand { get; } = null!;
        public DelegateCommand LoadDataCommand { get; } = null!;

        #endregion

        #region Constructor

        public EditFormulaDialogViewModel(
            IEventAggregator eventAggregator,
            IErrorHandlingService errorHandlingService,
            ICustomDialogService dialogService,
            IFormulaService formulaService,
            IHerbService herbService,
            ILogger<EditFormulaDialogViewModel> logger) 
            : base(eventAggregator, errorHandlingService)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await SaveFormulaAsync(), CanSave)
                .ObservesProperty(() => Formula)
                .ObservesProperty(() => HerbItems);
            CancelCommand = new DelegateCommand(Cancel);
            AddHerbCommand = new DelegateCommand(AddHerb);
            RemoveHerbCommand = new DelegateCommand<FormulaHerbItemDto>(RemoveHerb);
            EditHerbCommand = new DelegateCommand<FormulaHerbItemDto>(EditHerb);
            LoadDataCommand = new DelegateCommand(async () => await LoadFormulaAsync());

            // 加载可用药材
            Task.Run(async () => await LoadAvailableHerbsAsync());
        }

        #endregion

        #region Methods

        public void Initialize(Guid formulaId)
        {
            _formulaId = formulaId;
            Task.Run(async () => await LoadFormulaAsync());
        }

        private async Task LoadFormulaAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载验方数据...";

                var result = await _formulaService.GetByIdAsync(_formulaId);
                if (result.IsSuccess && result.Data != null)
                {
                    // UltraThink v2.0: 直接使用FormulaDto
                    Formula = result.Data;
                    // TODO: 需要根据实际的FormulaDto结构来处理药材项目
                    // 暂时创建空的药材项目列表
                    HerbItems = new ObservableCollection<FormulaHerbItemDto>();
                    StatusMessage = string.Empty;
                }
                else
                {
                    StatusMessage = result.ErrorMessage ?? "加载验方失败";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
                _logger.LogError(ex, "加载验方时出错");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadAvailableHerbsAsync()
        {
            try
            {
                var query = new HerbPagedQueryDto { PageIndex = 1, PageSize = 1000 };
                var herbsResult = await _herbService.GetPagedAsync(query);
                if (herbsResult.IsSuccess && herbsResult.Data?.Items != null)
                {
                    // UltraThink v2.0: 直接使用HerbDto
                    AvailableHerbs.Clear();
                    foreach (var herb in herbsResult.Data.Items)
                    {
                        AvailableHerbs.Add(herb);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载药材列表失败");
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Formula?.Name) && HerbItems.Count > 0;
        }

        private async Task SaveFormulaAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在保存验方...";

                // UltraThink v2.0: 直接使用FormulaDto属性创建UpdateDto
                var updateDto = new FormulaUpdateDto
                {
                    Id = Formula.Id,
                    Name = Formula.Name,
                    Effect = Formula.Effect ?? string.Empty,
                    Usage = Formula.Usage ?? string.Empty,
                    Remark = Formula.Remark,
                    Herbs = HerbItems.Select(h => new FormulaHerbItemUpdateDto
                    {
                        HerbId = h.HerbId,
                        Quantity = h.Quantity,
                        Preparation = h.Preparation,
                        Usage = h.Usage,
                        SortOrder = 0
                    }).ToList()
                };

                var result = await _formulaService.UpdateAsync(Formula.Id, updateDto);
                if (result.IsSuccess)
                {
                    StatusMessage = "验方保存成功";
                    // TODO: Close dialog with success
                }
                else
                {
                    StatusMessage = result.ErrorMessage ?? "保存失败";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存失败: {ex.Message}";
                _logger.LogError(ex, "保存验方时出错");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Cancel()
        {
            // TODO: Close dialog without saving
        }

        private async void AddHerb()
        {
            try
            {
                // 使用现有的药材选择对话框
                var parameters = new DialogParameters
                {
                    { "Title", "选择药材" },
                    { "AllowQuantityEdit", true }
                };

                var result = await _dialogService.ShowDialogAsync("HerbSelectionDialog", parameters);
                
                if (result?.Result == true && result.Parameters.ContainsKey("SelectedHerb"))
                {
                    var selectedHerb = result.Parameters["SelectedHerb"];
                    
                    // 根据选择结果创建FormulaHerbItemDto
                    if (selectedHerb != null)
                    {
                        // 如果返回的是HerbDto，需要转换为FormulaHerbItemDto
                        var herbDto = selectedHerb as HerbDto;
                        if (herbDto != null)
                        {
                            var newItem = new FormulaHerbItemDto
                            {
                                HerbId = herbDto.Id,
                                HerbName = herbDto.Name,
                                Quantity = result.Parameters.ContainsKey("Quantity") ? Convert.ToDecimal(result.Parameters["Quantity"]) : 10,
                                Unit = result.Parameters.ContainsKey("Unit") ? result.Parameters["Unit"]?.ToString() ?? "g" : "g",
                                Preparation = "煎服", // 默认处理方法
                                Usage = "" // 默认特殊说明
                            };
                            HerbItems.Add(newItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加药材时发生错误");
                StatusMessage = "添加药材失败：" + ex.Message;
            }
        }

        private void RemoveHerb(FormulaHerbItemDto? item)
        {
            if (item != null)
            {
                HerbItems.Remove(item);
            }
        }

        private void EditHerb(FormulaHerbItemDto? item)
        {
            if (item == null) return;
            
            // TODO: 实现编辑药材对话框
            StatusMessage = string.Format(SystemConstants.FeaturePendingTemplate, $"编辑药材 '{item.HerbName}'");
        }

        #endregion

        #region DialogViewModel Overrides

        protected override async Task<bool> SaveAsync()
        {
            await SaveFormulaAsync();
            return true;
        }

        #endregion

        #region ICustomDialogAware Implementation

        public string Title => "编辑验方";

        public event Action<CustomDialogResult>? RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed()
        {
            // 对话框关闭时的清理工作
        }

        public void OnDialogOpened(Dictionary<string, object> parameters)
        {
            if (parameters.ContainsKey("FormulaId") && parameters["FormulaId"] is Guid formulaId)
            {
                Initialize(formulaId);
            }
        }

        #endregion
    }
}