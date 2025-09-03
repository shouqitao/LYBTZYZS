using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using LYBT.Shared.Models.Contracts.Common;
using Prism.Events;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 验方详情视图模型 - UltraThink v2.0架构
    /// 提供验方详细信息查看和编辑功能
    /// </summary>
    public class FormulaDetailViewModel : ServiceViewModel, INavigationAware
    {
        #region 私有字段

        private readonly IFormulaService _formulaService;
        private readonly ICustomDialogService _dialogService;
        private readonly IRegionManager _regionManager;
        private readonly IMapper _mapper;

        private Guid _formulaId;
        private FormulaDto? _formula;
        private bool _isLoading;
        private bool _isReadOnly = true;

        #endregion

        #region 属性

        public Guid FormulaId
        {
            get => _formulaId;
            set => SetProperty(ref _formulaId, value);
        }

        public FormulaDto? Formula
        {
            get => _formula;
            set => SetProperty(ref _formula, value);
        }

        public new bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetProperty(ref _isReadOnly, value);
        }

        // 验方基本信息属性
        public string FormulaName => Formula?.Name ?? "";
        public string Effect => Formula?.Effect ?? "";
        public string Usage => Formula?.Usage ?? "";
        public string Property => Formula?.Property ?? "";
        public string Description => Formula?.Description ?? "";
        public string Difficulty => Formula?.Difficulty ?? "";
        public string Remark => Formula?.Remark ?? "";
        public bool IsShared => Formula?.IsShared ?? false;
        public DateTime? CreateTime => Formula?.CreateTime;
        public DateTime? UpdateTime => Formula?.UpdateTime;
        public string StatusText => GetStatusText();
        public int HerbCount => Formula?.HerbCount ?? 0;
        public decimal TotalPrice => Formula?.TotalPrice ?? 0;
        public string HerbNames => Formula?.GetHerbNamesList(5) ?? "暂无药材";
        public string Category => Formula?.Category ?? "未分类";

        // 药材组成
        public ObservableCollection<FormulaHerbItemDto> HerbItems { get; set; } = new();

        #endregion

        #region 命令

        public ICommand LoadDataCommand { get; } = null!;
        public ICommand BackCommand { get; } = null!;
        public ICommand EditCommand { get; } = null!;
        public ICommand SaveCommand { get; } = null!;
        public ICommand CancelEditCommand { get; } = null!;
        public ICommand PrintCommand { get; } = null!;
        public ICommand CopyFormulaCommand { get; } = null!;
        public ICommand ViewUsageHistoryCommand { get; } = null!;

        #endregion

        #region 构造函数

        public FormulaDetailViewModel(
            IFormulaService formulaService,
            ICustomDialogService dialogService,
            IRegionManager regionManager,
            IMapper mapper,
            IErrorHandlingService errorHandlingService,
            IEventAggregator eventAggregator)
            : base(eventAggregator, errorHandlingService)
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // 初始化命令
            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            BackCommand = new DelegateCommand(NavigateBack);
            EditCommand = new DelegateCommand(EnableEdit, CanEdit);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
            CancelEditCommand = new DelegateCommand(CancelEdit, CanCancelEdit);
            PrintCommand = new DelegateCommand(async () => await PrintFormulaAsync());
            CopyFormulaCommand = new DelegateCommand(async () => await CopyFormulaAsync());
            ViewUsageHistoryCommand = new DelegateCommand(async () => await ViewUsageHistoryAsync());
        }

        #endregion

        #region INavigationAware 实现

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("FormulaId"))
            {
                FormulaId = navigationContext.Parameters.GetValue<Guid>("FormulaId");
                
                if (navigationContext.Parameters.ContainsKey("ViewMode"))
                {
                    var viewMode = navigationContext.Parameters.GetValue<string>("ViewMode");
                    IsReadOnly = viewMode != "Edit";
                }

                Task.Run(async () => await LoadDataAsync());
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("FormulaId"))
            {
                var targetFormulaId = navigationContext.Parameters.GetValue<Guid>("FormulaId");
                return FormulaId == targetFormulaId;
            }
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            if (!IsReadOnly && HasUnsavedChanges())
            {
                // 可以在这里添加保存确认逻辑
            }
        }

        #endregion

        #region 数据操作

        private async Task LoadDataAsync()
        {
            if (FormulaId == Guid.Empty) return;

            try
            {
                IsLoading = true;

                var result = await _formulaService.GetByIdAsync(FormulaId);
                
                if (result.IsSuccess && result.Data != null)
                {
                    Formula = result.Data;
                    LoadHerbItems();
                    RefreshProperties();
                }
                else
                {
                    await _dialogService.ShowErrorAsync($"加载验方详情失败: {result.ErrorMessage}", "错误");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"加载验方详情失败: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveAsync()
        {
            if (Formula == null) return;

            try
            {
                IsLoading = true;

                // 更新药材组成
                Formula.Herbs = HerbItems.ToList();

                var updateDto = _mapper.Map<FormulaUpdateDto>(Formula);
                var result = await _formulaService.UpdateAsync(Formula.Id, updateDto);
                
                if (result.IsSuccess && result.Data != null)
                {
                    Formula = result.Data;
                    LoadHerbItems();
                    IsReadOnly = true;
                    RefreshProperties();
                    RaiseCanExecuteChanged();
                    
                    await _dialogService.ShowSuccessAsync("验方信息保存成功", "成功");
                }
                else
                {
                    await _dialogService.ShowErrorAsync($"保存失败: {result.ErrorMessage}", "错误");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"保存失败: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadHerbItems()
        {
            HerbItems.Clear();
            if (Formula?.Herbs != null)
            {
                foreach (var herb in Formula.Herbs)
                {
                    HerbItems.Add(herb);
                }
            }
        }

        #endregion

        #region 命令处理

        private void NavigateBack()
        {
            _regionManager.RequestNavigate(RegionNames.SystemWorkbenchContentRegion, "FormulaManagementView");
        }

        private void EnableEdit()
        {
            IsReadOnly = false;
            RaiseCanExecuteChanged();
        }

        private void CancelEdit()
        {
            IsReadOnly = true;
            // 重新加载数据以取消更改
            Task.Run(async () => await LoadDataAsync());
        }

        private async Task PrintFormulaAsync()
        {
            try
            {
                await _dialogService.ShowInformationAsync(
                    "验方打印功能将在后续版本中提供\n\n当前支持的操作：\n• 查看验方详情\n• 编辑验方信息\n• 复制验方\n• 查看药材组成", 
                    "功能说明");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"打印失败: {ex.Message}", "错误");
            }
        }

        private async Task CopyFormulaAsync()
        {
            if (Formula == null) return;
            
            try
            {
                var newName = $"{Formula.Name}_副本";
                // TODO: 简化后的接口暂不支持复制功能
                var result = ServiceResult<FormulaDto>.Failure("简单诊所版本暂不支持验方复制功能");
                
                if (result.IsSuccess)
                {
                    await _dialogService.ShowSuccessAsync("验方复制成功", "成功");
                }
                else
                {
                    await _dialogService.ShowErrorAsync($"复制失败: {result.ErrorMessage}", "错误");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"复制失败: {ex.Message}", "错误");
            }
        }

        private async Task ViewUsageHistoryAsync()
        {
            if (Formula == null) return;
            
            try
            {
                await _dialogService.ShowInformationAsync("使用历史功能正在开发中", "提示");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"操作失败: {ex.Message}", "错误");
            }
        }

        #endregion

        #region 命令状态

        private bool CanEdit() => Formula != null && IsReadOnly && !IsLoading;
        
        private bool CanSave() => Formula != null && !IsReadOnly && !IsLoading;
        
        private bool CanCancelEdit() => Formula != null && !IsReadOnly && !IsLoading;

        private new void RaiseCanExecuteChanged()
        {
            ((DelegateCommand)EditCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)SaveCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)CancelEditCommand).RaiseCanExecuteChanged();
        }

        #endregion

        #region 辅助方法

        private void RefreshProperties()
        {
            RaisePropertyChanged(nameof(FormulaName));
            RaisePropertyChanged(nameof(Effect));
            RaisePropertyChanged(nameof(Usage));
            RaisePropertyChanged(nameof(Property));
            RaisePropertyChanged(nameof(Description));
            RaisePropertyChanged(nameof(Difficulty));
            RaisePropertyChanged(nameof(Remark));
            RaisePropertyChanged(nameof(IsShared));
            RaisePropertyChanged(nameof(CreateTime));
            RaisePropertyChanged(nameof(UpdateTime));
            RaisePropertyChanged(nameof(StatusText));
            RaisePropertyChanged(nameof(HerbCount));
            RaisePropertyChanged(nameof(TotalPrice));
            RaisePropertyChanged(nameof(HerbNames));
            RaisePropertyChanged(nameof(Category));
        }

        private string GetStatusText()
        {
            if (Formula?.Status == CommonStatus.Enabled)
                return "正常";
            return "已禁用";
        }

        private bool HasUnsavedChanges()
        {
            // 简单实现：如果处于编辑模式就认为有未保存的更改
            return !IsReadOnly;
        }

        #endregion
    }
}