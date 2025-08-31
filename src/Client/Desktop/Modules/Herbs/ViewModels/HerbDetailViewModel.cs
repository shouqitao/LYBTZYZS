using System;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Events;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Herbs.ViewModels
{
    /// <summary>
    /// 中药材详情视图模型 - UltraThink ModernViewModel架构
    /// 提供中药材详细信息查看和编辑功能
    /// </summary>
    public class HerbDetailViewModel : ModernViewModelBase, INavigationAware
    {
        #region 私有字段

        private readonly IHerbService _herbService;
        private readonly ICustomDialogService _dialogService;
        private readonly IRegionManager _regionManager;
        private readonly IMapper _mapper;

        private Guid _herbId;
        private HerbDto? _herb;
        private bool _isReadOnly = true;

        #endregion

        #region 属性

        public Guid HerbId
        {
            get => _herbId;
            set => SetProperty(ref _herbId, value);
        }

        public HerbDto? Herb
        {
            get => _herb;
            set => SetProperty(ref _herb, value);
        }


        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetProperty(ref _isReadOnly, value);
        }

        // 中药材基本信息属性
        public string HerbName => Herb?.Name ?? "";
        public string PinYinCode => Herb?.PinYinCode ?? "";
        public string Origin => Herb?.Origin ?? "";
        public string Spec => Herb?.Spec ?? "";
        public string Unit => Herb?.Unit ?? "";
        public decimal Price => Herb?.Price ?? 0;
        public string Effect => Herb?.Effect ?? "";
        public string Usage => Herb?.Usage ?? "";
        public string Remark => Herb?.Remark ?? "";
        public DateTime? CreateTime => Herb?.CreateTime;
        public DateTime? UpdateTime => Herb?.UpdateTime;
        public string StatusText => GetStatusText();

        #endregion

        #region 命令

        public DelegateCommand LoadDataCommand { get; }
        public DelegateCommand BackCommand { get; }
        public DelegateCommand EditCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelEditCommand { get; }
        public DelegateCommand PrintCommand { get; }
        public DelegateCommand ViewUsageHistoryCommand { get; }

        #endregion

        #region 构造函数

        public HerbDetailViewModel(
            IHerbService herbService,
            ICustomDialogService dialogService,
            IRegionManager regionManager,
            IMapper mapper,
            IErrorHandlingService errorHandlingService,
            IEventAggregator eventAggregator)
            : base(eventAggregator, errorHandlingService)
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // 初始化命令
            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            BackCommand = new DelegateCommand(NavigateBack);
            EditCommand = new DelegateCommand(EnableEdit, CanEdit);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
            CancelEditCommand = new DelegateCommand(CancelEdit, CanCancelEdit);
            PrintCommand = new DelegateCommand(async () => await PrintHerbAsync());
            ViewUsageHistoryCommand = new DelegateCommand(async () => await ViewUsageHistoryAsync());
        }

        #endregion

        #region INavigationAware 实现

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("HerbId"))
            {
                HerbId = navigationContext.Parameters.GetValue<Guid>("HerbId");
                
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
            if (navigationContext.Parameters.ContainsKey("HerbId"))
            {
                var targetHerbId = navigationContext.Parameters.GetValue<Guid>("HerbId");
                return HerbId == targetHerbId;
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
            if (HerbId == Guid.Empty) return;

            await ExecuteAsync(async () =>
            {
                var result = await _herbService.GetByIdAsync(HerbId);
                
                if (result.IsSuccess && result.Data != null)
                {
                    Herb = result.Data;
                    RefreshProperties();
                }
                else
                {
                    await _dialogService.ShowErrorAsync($"加载中药材详情失败: {result.ErrorMessage}", "错误");
                }
            }, "加载中药材详情");
        }

        private async Task SaveAsync()
        {
            if (Herb == null) return;

            await ExecuteAsync(async () =>
            {
                var updateDto = _mapper.Map<HerbUpdateDto>(Herb);
                
                var result = await _herbService.UpdateAsync(Herb.Id, updateDto);
                
                if (result.IsSuccess && result.Data != null)
                {
                    Herb = result.Data;
                    IsReadOnly = true;
                    RefreshProperties();
                    RaiseCanExecuteChanged();
                    
                    await _dialogService.ShowSuccessAsync("中药材信息保存成功", "成功");
                }
                else
                {
                    await _dialogService.ShowErrorAsync($"保存失败: {result.ErrorMessage}", "错误");
                }
            }, "保存中药材信息");
        }

        #endregion

        #region 命令处理

        private void NavigateBack()
        {
            _regionManager.RequestNavigate(RegionNames.SystemWorkbenchContentRegion, "HerbManagementView");
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

        private async Task PrintHerbAsync()
        {
            await ExecuteAsync(async () =>
            {
                await _dialogService.ShowInformationAsync("打印功能正在开发中", "提示");
            }, "打印中药材信息");
        }

        private async Task ViewUsageHistoryAsync()
        {
            if (Herb == null) return;
            
            await ExecuteAsync(async () =>
            {
                await _dialogService.ShowInformationAsync("使用历史功能正在开发中", "提示");
            }, "查看使用历史");
        }

        #endregion

        #region 命令状态

        private bool CanEdit() => Herb != null && IsReadOnly && !base.IsLoading;
        
        private bool CanSave() => Herb != null && !IsReadOnly && !base.IsLoading;
        
        private bool CanCancelEdit() => Herb != null && !IsReadOnly && !base.IsLoading;

        protected override void RaiseCanExecuteChanged()
        {
            base.RaiseCanExecuteChanged();
            EditCommand?.RaiseCanExecuteChanged();
            SaveCommand?.RaiseCanExecuteChanged();
            CancelEditCommand?.RaiseCanExecuteChanged();
            LoadDataCommand?.RaiseCanExecuteChanged();
            BackCommand?.RaiseCanExecuteChanged();
            PrintCommand?.RaiseCanExecuteChanged();
            ViewUsageHistoryCommand?.RaiseCanExecuteChanged();
        }

        #endregion

        #region 辅助方法

        private void RefreshProperties()
        {
            RaisePropertyChanged(nameof(HerbName));
            RaisePropertyChanged(nameof(PinYinCode));
            RaisePropertyChanged(nameof(Origin));
            RaisePropertyChanged(nameof(Spec));
            RaisePropertyChanged(nameof(Unit));
            RaisePropertyChanged(nameof(Price));
            RaisePropertyChanged(nameof(Effect));
            RaisePropertyChanged(nameof(Usage));
            RaisePropertyChanged(nameof(Remark));
            RaisePropertyChanged(nameof(CreateTime));
            RaisePropertyChanged(nameof(UpdateTime));
            RaisePropertyChanged(nameof(StatusText));
        }

        private string GetStatusText()
        {
            if (Herb?.Status == CommonStatus.Enabled)
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