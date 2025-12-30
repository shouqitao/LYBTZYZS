using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Http;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 详情编辑ViewModel基类
    /// OpenSpec: migrate-to-communitytoolkit-mvvm
    ///
    /// 适用于:
    /// - 单实体详情查看/编辑页面
    /// - 表单编辑页面
    /// - 需要保存/取消操作的页面
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public abstract partial class DetailViewModelBase<T> : PageViewModelBase
        where T : class, new()
    {
        #region 可观察属性

        /// <summary>
        /// 当前编辑的实体
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
        [NotifyPropertyChangedFor(nameof(HasItem))]
        private T? _currentItem;

        /// <summary>
        /// 是否为新建实体
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormTitle))]
        private bool _isNewItem;

        /// <summary>
        /// 是否处于编辑模式
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
        [NotifyCanExecuteChangedFor(nameof(StartEditCommand))]
        [NotifyPropertyChangedFor(nameof(IsReadOnly))]
        private bool _isEditing;

        /// <summary>
        /// 实体ID（用于加载和保存）
        /// </summary>
        [ObservableProperty]
        private Guid _entityId;

        #endregion

        #region 计算属性

        /// <summary>
        /// 是否有实体数据
        /// </summary>
        public bool HasItem => CurrentItem != null;

        /// <summary>
        /// 是否只读模式
        /// </summary>
        public bool IsReadOnly => !IsEditing;

        /// <summary>
        /// 表单标题（根据新建/编辑状态）
        /// </summary>
        public virtual string FormTitle => IsNewItem ? "新建" : "编辑";

        #endregion

        #region 构造函数

        protected DetailViewModelBase(
            IRegionManager regionManager,
            ICommonDialogService dialogService,
            IApiService apiService,
            ISessionManager sessionManager,
            ILoggerFactory loggerFactory)
            : base(regionManager, dialogService, apiService, sessionManager, loggerFactory)
        {
        }

        #endregion

        #region 命令

        /// <summary>
        /// 保存命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSave))]
        protected virtual async Task SaveAsync()
        {
            if (CurrentItem == null) return;

            // 先验证
            if (!ValidateBeforeSave())
            {
                await ShowWarningMessageAsync("请检查输入数据");
                return;
            }

            var success = await SafeExecuteAsync(async () =>
            {
                await SaveItemAsync(CurrentItem);
                return true;
            }, "保存");

            if (success)
            {
                await ShowSuccessMessageAsync("保存成功");
                IsEditing = false;
                await OnSaveSuccessAsync();
            }
        }

        /// <summary>
        /// 是否可以保存
        /// </summary>
        protected virtual bool CanSave => IsEditing && CurrentItem != null && !HasErrors && !IsBusy;

        /// <summary>
        /// 取消命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCancel))]
        protected virtual async Task CancelAsync()
        {
            if (IsNewItem)
            {
                // 新建时取消，直接返回
                await OnCancelNewAsync();
            }
            else
            {
                // 编辑时取消，重新加载数据
                await ReloadItemAsync();
            }

            IsEditing = false;
        }

        /// <summary>
        /// 是否可以取消
        /// </summary>
        protected virtual bool CanCancel => IsEditing && !IsBusy;

        /// <summary>
        /// 开始编辑命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanStartEdit))]
        protected virtual void StartEdit()
        {
            IsEditing = true;
            Logger.LogDebug("进入编辑模式: {EntityId}", EntityId);
        }

        /// <summary>
        /// 是否可以开始编辑
        /// </summary>
        protected virtual bool CanStartEdit => !IsEditing && CurrentItem != null && !IsBusy;

        /// <summary>
        /// 删除命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDelete))]
        protected virtual async Task DeleteAsync()
        {
            if (CurrentItem == null) return;

            var confirmed = await ShowConfirmationAsync("确定要删除吗？此操作不可撤销。", "确认删除");
            if (!confirmed) return;

            var success = await SafeExecuteAsync(async () =>
            {
                await DeleteItemAsync(EntityId);
                return true;
            }, "删除");

            if (success)
            {
                await ShowSuccessMessageAsync("删除成功");
                await OnDeleteSuccessAsync();
            }
        }

        /// <summary>
        /// 是否可以删除
        /// </summary>
        protected virtual bool CanDelete => CurrentItem != null && !IsNewItem && !IsBusy;

        #endregion

        #region 属性变更回调

        /// <summary>
        /// CurrentItem变更时调用
        /// </summary>
        partial void OnCurrentItemChanged(T? value)
        {
            OnCurrentItemChangedCore(value);
        }

        /// <summary>
        /// 派生类可重写以响应CurrentItem变更
        /// </summary>
        protected virtual void OnCurrentItemChangedCore(T? value)
        {
            ClearAllErrors();
        }

        /// <summary>
        /// IsEditing变更时调用
        /// </summary>
        partial void OnIsEditingChanged(bool value)
        {
            OnIsEditingChangedCore(value);
        }

        /// <summary>
        /// 派生类可重写以响应编辑模式变更
        /// </summary>
        protected virtual void OnIsEditingChangedCore(bool value)
        {
            if (!value)
            {
                ClearAllErrors();
            }
        }

        #endregion

        #region 抽象方法（子类必须实现）

        /// <summary>
        /// 加载实体数据
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <returns>实体对象</returns>
        protected abstract Task<T?> LoadItemAsync(Guid id);

        /// <summary>
        /// 保存实体数据
        /// </summary>
        /// <param name="item">要保存的实体</param>
        protected abstract Task SaveItemAsync(T item);

        #endregion

        #region 虚方法（子类可选实现）

        /// <summary>
        /// 删除实体
        /// </summary>
        protected virtual Task DeleteItemAsync(Guid id) => Task.CompletedTask;

        /// <summary>
        /// 保存前验证
        /// </summary>
        /// <returns>验证是否通过</returns>
        protected virtual bool ValidateBeforeSave()
        {
            ValidateAllPropertiesAndCheck();
            return !HasErrors;
        }

        /// <summary>
        /// 保存成功后回调
        /// </summary>
        protected virtual Task OnSaveSuccessAsync() => Task.CompletedTask;

        /// <summary>
        /// 删除成功后回调
        /// </summary>
        protected virtual Task OnDeleteSuccessAsync() => Task.CompletedTask;

        /// <summary>
        /// 新建取消时回调
        /// </summary>
        protected virtual Task OnCancelNewAsync() => Task.CompletedTask;

        /// <summary>
        /// 重新加载实体
        /// </summary>
        protected virtual async Task ReloadItemAsync()
        {
            if (EntityId == Guid.Empty) return;

            var item = await SafeExecuteAsync(
                () => LoadItemAsync(EntityId),
                "重新加载数据");

            if (item != null)
            {
                CurrentItem = item;
            }
        }

        /// <summary>
        /// 创建新实体
        /// </summary>
        protected virtual T CreateNewItem() => new T();

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化为新建模式
        /// </summary>
        public virtual void InitializeForNew()
        {
            EntityId = Guid.Empty;
            IsNewItem = true;
            IsEditing = true;
            CurrentItem = CreateNewItem();
            Logger.LogDebug("初始化新建模式");
        }

        /// <summary>
        /// 初始化为编辑模式
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <param name="autoStartEdit">是否自动进入编辑状态</param>
        public virtual async Task InitializeForEditAsync(Guid id, bool autoStartEdit = false)
        {
            EntityId = id;
            IsNewItem = false;
            IsEditing = autoStartEdit;

            var item = await SafeExecuteAsync(
                () => LoadItemAsync(id),
                "加载数据");

            if (item != null)
            {
                CurrentItem = item;
                Logger.LogDebug("初始化编辑模式: {EntityId}", id);
            }
            else
            {
                Logger.LogWarning("未找到实体: {EntityId}", id);
                await ShowErrorMessageAsync("未找到数据");
            }
        }

        #endregion

        #region 导航参数处理

        /// <summary>
        /// 处理导航参数
        /// </summary>
        protected override void ProcessNavigationParameters(NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);

            if (parameters.TryGetValue<Guid>("Id", out var id) && id != Guid.Empty)
            {
                EntityId = id;
                IsNewItem = false;
            }
            else if (parameters.TryGetValue<bool>("IsNew", out var isNew) && isNew)
            {
                IsNewItem = true;
            }
        }

        /// <summary>
        /// 初始化（根据导航参数）
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            if (IsNewItem)
            {
                InitializeForNew();
            }
            else if (EntityId != Guid.Empty)
            {
                await InitializeForEditAsync(EntityId);
            }
        }

        #endregion
    }
}
