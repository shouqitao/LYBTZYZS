using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Formulas;
using LYBT.Desktop.Core.Models.Herbs;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Extensions;
using Prism.Commands;
using Prism.Mvvm;
using Microsoft.Extensions.Logging;

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

        private FormulaInfo _formula = new();
        public FormulaInfo Formula
        {
            get => _formula;
            set => SetProperty(ref _formula, value);
        }

        private ObservableCollection<FormulaHerbItem> _herbItems = new();
        public ObservableCollection<FormulaHerbItem> HerbItems
        {
            get => _herbItems;
            set => SetProperty(ref _herbItems, value);
        }

        private FormulaHerbItem? _selectedHerbItem;
        public FormulaHerbItem? SelectedHerbItem
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

        public ObservableCollection<HerbInfo> AvailableHerbs { get; } = new();

        #endregion

        #region Commands

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand AddHerbCommand { get; }
        public DelegateCommand<FormulaHerbItem> RemoveHerbCommand { get; }
        public DelegateCommand<FormulaHerbItem> EditHerbCommand { get; }
        public DelegateCommand LoadDataCommand { get; }

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
            RemoveHerbCommand = new DelegateCommand<FormulaHerbItem>(RemoveHerb);
            EditHerbCommand = new DelegateCommand<FormulaHerbItem>(EditHerb);
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
                    // Convert FormulaDto to FormulaInfo using extension method
                    Formula = result.Data.ToFormulaInfo();
                    if (Formula.Herbs != null)
                    {
                        HerbItems = new ObservableCollection<FormulaHerbItem>(Formula.Herbs);
                    }
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
                var herbsResult = await _herbService.GetHerbsAsync();
                if (herbsResult.IsSuccess && herbsResult.Data != null)
                {
                    AvailableHerbs.Clear();
                    foreach (var herb in herbsResult.Data)
                    {
                        AvailableHerbs.Add(herb.ToHerbInfo());
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

                // 创建UpdateDto
                var updateDto = new FormulaUpdateDto
                {
                    Id = Formula.Id,
                    Name = Formula.Name,
                    Effect = Formula.Indications ?? string.Empty,
                    Usage = Formula.DosageInstruction ?? string.Empty,
                    Remark = Formula.Remark,
                    Herbs = HerbItems.Select(h => new FormulaHerbItemUpdateDto
                    {
                        HerbId = h.HerbId,
                        Quantity = h.Quantity,
                        Preparation = h.ProcessingMethod,
                        Usage = h.SpecialInstructions,
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
            var newItem = new FormulaHerbItem
            {
                HerbId = Guid.NewGuid(),
                HerbName = "新药材",
                Quantity = 10,
                Unit = "克",
                ProcessingMethod = "煎服"
            };
            HerbItems.Add(newItem);
        }

        private void RemoveHerb(FormulaHerbItem? item)
        {
            if (item != null)
            {
                HerbItems.Remove(item);
            }
        }

        private void EditHerb(FormulaHerbItem? item)
        {
            if (item == null) return;
            
            // TODO: 实现编辑药材对话框
            StatusMessage = $"编辑药材 '{item.HerbName}' 功能待实现";
        }

        #endregion
    }
}