using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Constants;
using LYBT.Shared.Interfaces.Services;
// UltraThink v2.0: 直接使用DTOs，移除Info模型引用
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 编辑验方对话框视图模型
    /// </summary>
    public class EditFormulaDialogViewModel : BindableBase
    {
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
            IFormulaService formulaService,
            IHerbService herbService,
            ILogger<EditFormulaDialogViewModel> logger)
        {
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

        private void AddHerb()
        {
            // TODO: 实现添加药材对话框
            var newItem = new FormulaHerbItemDto
            {
                HerbId = Guid.NewGuid(),
                HerbName = "新药材",
                Quantity = 10,
                Unit = "克",
                Preparation = "煎服"
            };
            HerbItems.Add(newItem);
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
            if (item == null)
            {
                return;
            }

            // TODO: 实现编辑药材对话框
            StatusMessage = string.Format(SystemConstants.FeaturePendingTemplate, $"编辑药材 '{item.HerbName}'");
        }

        #endregion
    }
}
