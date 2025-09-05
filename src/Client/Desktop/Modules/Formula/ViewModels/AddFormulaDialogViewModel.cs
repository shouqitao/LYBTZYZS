using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
// UltraThink v2.0: 直接使用DTOs，移除Info模型引用
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 新增验方对话框视图模型
    /// </summary>
    public class AddFormulaDialogViewModel : DialogViewModel, ICustomDialogAware
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


        public ObservableCollection<string> Categories { get; } = new()
        {
            "内科方", "外科方", "妇科方", "儿科方",
            "皮肤科方", "五官科方", "骨伤科方", "经典方",
            "时方", "验方", "其他"
        };

        public ObservableCollection<HerbDto> AvailableHerbs { get; } = new();

        #endregion

        #region Commands

        public DelegateCommand AddHerbCommand { get; } = null!;
        public DelegateCommand<FormulaHerbItemDto> RemoveHerbCommand { get; } = null!;
        public DelegateCommand LoadHerbsCommand { get; } = null!;

        #endregion

        #region Constructor

        /// <summary>
        /// 主构造函数（支持可选错误处理服务）
        /// </summary>
        public AddFormulaDialogViewModel(
            IFormulaService formulaService,
            IHerbService herbService,
            ILogger<AddFormulaDialogViewModel> logger,
            IEventAggregator eventAggregator,
            IErrorHandlingService? errorHandlingService = null) : base(eventAggregator, errorHandlingService)
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 初始化自定义命令
            AddHerbCommand = new DelegateCommand(AddHerb);
            RemoveHerbCommand = new DelegateCommand<FormulaHerbItemDto>(RemoveHerb);
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
                var query = new HerbPagedQueryDto { PageIndex = 1, PageSize = 1000 };
                var herbsResult = await _herbService.GetPagedAsync(query);
                if (herbsResult.IsSuccess && herbsResult.Data?.Items != null)
                {
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
            finally
            {
                IsLoading = false;
            }
        }

        protected override bool CanSave()
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
                        Preparation = h.Preparation,
                        Usage = h.Usage,
                        SortOrder = 0
                    }).ToList()
                };

                var result = await _formulaService.CreateAsync(createDto);
                if (result.IsSuccess)
                {
                    StatusMessage = "验方保存成功";
                    RaiseRequestClose(true);
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


        private void AddHerb()
        {
            // TODO: 实现添加药材对话框
            // 暂时添加一个示例药材
            var newItem = new FormulaHerbItemDto
            {
                HerbId = Guid.NewGuid(),
                HerbName = "示例药材",
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

        #endregion

        #region ICustomDialogAware Implementation

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title => "新增验方";

        /// <summary>
        /// 请求关闭对话框事件
        /// </summary>
        public event Action<CustomDialogResult> RequestClose = delegate { };

        /// <summary>
        /// 检查是否可以关闭对话框
        /// </summary>
        public bool CanCloseDialog()
        {
            return !IsLoading;
        }

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        public void OnDialogOpened(Dictionary<string, object> parameters)
        {
            // 如果需要从参数初始化数据
            // 目前新增验方不需要参数
        }

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        public void OnDialogClosed()
        {
            // 清理资源
        }

        /// <summary>
        /// 实现抽象方法 - 保存数据
        /// </summary>
        protected override async Task<bool> SaveAsync()
        {
            try
            {
                await SaveFormulaAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 请求关闭对话框
        /// </summary>
        /// <param name="dialogResult">对话框结果</param>
        protected void RaiseRequestClose(bool? dialogResult)
        {
            if (CanCloseDialog())
            {
                var result = dialogResult == true
                    ? CustomDialogResult.Success(null)
                    : CustomDialogResult.Cancel();

                RequestClose?.Invoke(result);
            }
        }

        #endregion
    }
}
