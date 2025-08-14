using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models;
using LYBT.Desktop.Core.Models.Formulas;
using LYBT.Desktop.Core.Models.Herbs;
using LYBT.Shared.Models.Contracts.Formula;
using Prism.Commands;
using Prism.Mvvm;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 新增验方对话框视图模型
    /// </summary>
    public class AddFormulaDialogViewModel : BindableBase
    {
        private readonly IFormulaService _formulaService;
        private readonly IHerbService _herbService;
        private readonly ILogger<AddFormulaDialogViewModel> _logger;

        #region Properties

        private string _formulaName = string.Empty;
        public string FormulaName
        {
            get => _formulaName;
            set => SetProperty(ref _formulaName, value);
        }

        private string _selectedCategory = "其他";
        public string SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        private string _indications = string.Empty;
        public string Indications
        {
            get => _indications;
            set => SetProperty(ref _indications, value);
        }

        private string _dosageInstruction = string.Empty;
        public string DosageInstruction
        {
            get => _dosageInstruction;
            set => SetProperty(ref _dosageInstruction, value);
        }

        private string _source = string.Empty;
        public string Source
        {
            get => _source;
            set => SetProperty(ref _source, value);
        }

        private string _remark = string.Empty;
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
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
        public DelegateCommand LoadHerbsCommand { get; }

        #endregion

        #region Constructor

        public AddFormulaDialogViewModel(
            IFormulaService formulaService,
            IHerbService herbService,
            ILogger<AddFormulaDialogViewModel> logger)
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await SaveFormulaAsync(), CanSave)
                .ObservesProperty(() => FormulaName)
                .ObservesProperty(() => HerbItems);
            CancelCommand = new DelegateCommand(Cancel);
            AddHerbCommand = new DelegateCommand(AddHerb);
            RemoveHerbCommand = new DelegateCommand<FormulaHerbItem>(RemoveHerb);
            LoadHerbsCommand = new DelegateCommand(async () => await LoadAvailableHerbsAsync());

            // 加载可用药材
            Task.Run(async () => await LoadAvailableHerbsAsync());
        }

        #endregion

        #region Methods

        private async Task LoadAvailableHerbsAsync()
        {
            try
            {
                IsLoading = true;
                var herbs = await _herbService.GetHerbsAsync();
                if (herbs != null)
                {
                    AvailableHerbs.Clear();
                    foreach (var herb in herbs)
                    {
                        AvailableHerbs.Add(herb);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载药材列表失败");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(FormulaName) && HerbItems.Count > 0;
        }

        private async Task SaveFormulaAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在保存验方...";

                var createDto = new FormulaCreateDto
                {
                    Name = FormulaName.Trim(),
                    Effect = Indications.Trim(),
                    Usage = DosageInstruction.Trim(),
                    Remark = Remark.Trim(),
                    Herbs = HerbItems.Select(h => new FormulaHerbItemCreateDto
                    {
                        HerbId = h.HerbId,
                        Quantity = h.Quantity,
                        Preparation = h.ProcessingMethod,
                        Usage = h.SpecialInstructions,
                        SortOrder = 0
                    }).ToList()
                };

                var result = await _formulaService.CreateAsync(createDto);
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
            // 暂时添加一个示例药材
            var newItem = new FormulaHerbItem
            {
                HerbId = Guid.NewGuid(),
                HerbName = "示例药材",
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

        #endregion
    }
}