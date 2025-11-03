# 组件化MVVM模式 - 快速参考

> **文档类型**: 快速参考（Quick Reference）
> **目标读者**: 开发者
> **Epic**: #1773 Desktop端MVVM架构优化 - 组件化模式推广
> **最后更新**: 2025-11-03

---

## 📋 目录

1. [组件化架构概览](#1-组件化架构概览)
2. [DataManager代码模板](#2-datamanager代码模板)
3. [CommandHandler代码模板](#3-commandhandler代码模板)
4. [Validator代码模板](#4-validator代码模板)
5. [ViewModel集成模式](#5-viewmodel集成模式)
6. [DI注册模式](#6-di注册模式)
7. [常见问题解决](#7-常见问题解决)

---

## 1. 组件化架构概览

### 1.1 设计理念

将ViewModel的核心职责拆分为**三个标准组件**：

```
┌─────────────────────────────────────────────────────────┐
│                  ViewModel（协调层）                      │
│  - 导航生命周期                                           │
│  - UI属性委托                                             │
│  - 事件订阅                                               │
└───────┬─────────────────┬─────────────────┬─────────────┘
        │                 │                 │
    ┌───▼────┐      ┌────▼─────┐      ┌───▼────┐
    │ Data   │      │ Command  │      │ Valid- │
    │ Manager│      │ Handler  │      │ ator   │
    └────────┘      └──────────┘      └────────┘
        │                 │                 │
        │                 │                 │
    ┌───▼────┐      ┌────▼─────┐      ┌───▼─────────┐
    │Reposit-│      │ Data     │      │FluentValid- │
    │ ory    │      │ Manager  │      │ation        │
    └────────┘      └──────────┘      └─────────────┘
```

### 1.2 组件职责表

| 组件 | 职责 | 代码量 | 生命周期 |
|-----|------|-------|---------|
| **DataManager** | 数据CRUD、状态管理、变更检测 | 150-350行 | Scoped |
| **CommandHandler** | 命令处理、业务逻辑、事件发布 | 120-400行 | Scoped |
| **Validator** | 集成FluentValidation、验证规则 | 80-180行 | Scoped |
| **ViewModel** | 导航、UI属性委托、事件订阅 | 精简后100-200行 | Scoped |

### 1.3 组件化覆盖情况

**当前覆盖率**: **75%** (6/8模块)

| 模块 | 状态 | 组件 |
|-----|------|------|
| Prescription | ✅ | DataManager + CommandHandler + Validator |
| Formula | ✅ | DataManager + CommandHandler + Validator |
| Patients | ✅ | DataManager + CommandHandler + Validator |
| MedicalCase | ✅ | DataManager + CommandHandler + Validator |
| Consultation | ✅ | DataManager + CommandHandler + Validator |
| Users | ✅ | DataManager + CommandHandler + Validator |
| Herbs | ⏳ | 业务相对简单，暂未组件化 |
| Auth | ⏳ | 业务功能单一，暂未组件化 |

---

## 2. DataManager代码模板

### 2.1 接口定义

**位置**: `LYBT.Desktop.Infrastructure/Interfaces/Components/IDataManager.cs`

```csharp
/// <summary>
/// 数据管理器接口 - 组件化MVVM架构核心接口
/// </summary>
/// <typeparam name="TDto">实体DTO类型</typeparam>
public interface IDataManager<TDto> where TDto : class
{
    /// <summary>当前实体数据</summary>
    TDto? Current { get; }

    /// <summary>是否有未保存的变更</summary>
    bool HasChanges { get; }

    /// <summary>初始化数据（加载现有数据或创建新数据）</summary>
    Task InitializeAsync(Guid entityId);

    /// <summary>保存数据（创建或更新）</summary>
    Task<bool> SaveAsync();

    /// <summary>删除数据</summary>
    Task<bool> DeleteAsync();

    /// <summary>重新加载数据</summary>
    Task ReloadAsync();
}
```

### 2.2 实现模板

**位置**: `LYBT.Desktop.{Module}/ViewModels/Components/{Entity}DataManager.cs`

```csharp
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.{Module}.Interfaces;
using LYBT.Shared.Models.Contracts.{Module};
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.{Module}.ViewModels.Components
{
    /// <summary>
    /// {Entity}数据管理器 - 组件化架构
    /// 职责单一：专注{Entity}数据的CRUD操作和状态管理
    /// Epic #1773 Task X: {Module}模块组件化改造
    /// </summary>
    public class {Entity}DataManager
    {
        #region 依赖注入

        private readonly I{Entity}Repository _{entityLowerCase}Repository;
        private readonly ILogger<{Entity}DataManager> _logger;
        private readonly IEventAggregator _eventAggregator;
        private readonly ISessionManager? _sessionManager;

        public {Entity}DataManager(
            I{Entity}Repository {entityLowerCase}Repository,
            ILogger<{Entity}DataManager> logger,
            IEventAggregator eventAggregator,
            ISessionManager? sessionManager = null)
        {
            _{entityLowerCase}Repository = {entityLowerCase}Repository ?? throw new ArgumentNullException(nameof({entityLowerCase}Repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _sessionManager = sessionManager;
        }

        #endregion 依赖注入

        #region 核心数据属性

        /// <summary>{Entity}ID</summary>
        public Guid {Entity}Id { get; private set; }

        /// <summary>当前{Entity}数据</summary>
        public {Entity}Dto? Current{Entity} { get; private set; }

        /// <summary>原始{Entity}数据（用于变更检测）</summary>
        private {Entity}Dto? _original{Entity};

        /// <summary>是否为新{Entity}</summary>
        public bool IsNew{Entity} { get; private set; } = true;

        /// <summary>是否正在加载</summary>
        public bool IsLoading { get; private set; }

        /// <summary>是否有未保存的变更</summary>
        public bool HasChanges { get; private set; }

        /// <summary>是否只读模式</summary>
        public bool IsReadOnly { get; set; } = true;

        #endregion 核心数据属性

        #region 数据初始化

        /// <summary>
        /// 初始化{Entity}数据
        /// </summary>
        /// <param name="{entityLowerCase}Id">{Entity}ID，如果为Empty则创建新{Entity}</param>
        public async Task InitializeAsync(Guid {entityLowerCase}Id)
        {
            try
            {
                IsLoading = true;
                {Entity}Id = {entityLowerCase}Id;

                _logger.LogInformation("开始初始化{Entity}数据，{Entity}ID: {{EntityId}}", {entityLowerCase}Id);

                if ({entityLowerCase}Id == Guid.Empty)
                {
                    // 新建{Entity}模式
                    IsNew{Entity} = true;
                    Current{Entity} = null;
                    _original{Entity} = null;
                    IsReadOnly = false;
                    _logger.LogInformation("初始化为新建{Entity}模式");
                }
                else
                {
                    // 加载现有{Entity}
                    await LoadExisting{Entity}Async();
                }

                HasChanges = false;
                _logger.LogInformation("{Entity}数据初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化{Entity}数据失败");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 加载现有{Entity}数据
        /// </summary>
        private async Task LoadExisting{Entity}Async()
        {
            try
            {
                _logger.LogInformation("开始加载{Entity}数据，{Entity}ID: {{EntityId}}", {Entity}Id);

                Current{Entity} = await _{entityLowerCase}Repository.GetByIdAsync({Entity}Id);

                if (Current{Entity} != null)
                {
                    // 保存原始数据副本用于变更检测
                    _original{Entity} = Clone{Entity}(Current{Entity});
                    IsNew{Entity} = false;
                    _logger.LogInformation("成功加载{Entity}数据: {{{Entity}Name}}", Current{Entity}.Name);
                }
                else
                {
                    _logger.LogWarning("未找到{Entity}数据，{Entity}ID: {{EntityId}}", {Entity}Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载{Entity}数据失败");
                throw;
            }
        }

        #endregion 数据初始化

        #region 数据操作

        /// <summary>
        /// 保存{Entity}数据
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            try
            {
                if (Current{Entity} == null)
                {
                    _logger.LogWarning("{Entity}数据为空，无法保存");
                    return false;
                }

                IsLoading = true;
                _logger.LogInformation("开始保存{Entity}数据");

                // 转换为InputDto
                var inputDto = ConvertToInputDto(Current{Entity});

                {Entity}Dto? saved{Entity};
                if (IsNew{Entity})
                {
                    saved{Entity} = await _{entityLowerCase}Repository.CreateAsync(inputDto);
                    _logger.LogInformation("新建{Entity}成功: {{{Entity}Name}}", saved{Entity}.Name);
                }
                else
                {
                    saved{Entity} = await _{entityLowerCase}Repository.UpdateAsync(inputDto);
                    _logger.LogInformation("更新{Entity}成功: {{{Entity}Name}}", saved{Entity}.Name);
                }

                if (saved{Entity} != null)
                {
                    Current{Entity} = saved{Entity};
                    _original{Entity} = Clone{Entity}(saved{Entity});
                    {Entity}Id = saved{Entity}.Id;
                    IsNew{Entity} = false;
                    HasChanges = false;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存{Entity}数据失败");
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 删除{Entity}数据
        /// </summary>
        public async Task<bool> DeleteAsync()
        {
            try
            {
                if ({Entity}Id == Guid.Empty)
                {
                    _logger.LogWarning("{Entity}ID为空，无法删除");
                    return false;
                }

                IsLoading = true;
                _logger.LogInformation("开始删除{Entity}，{Entity}ID: {{EntityId}}", {Entity}Id);

                var result = await _{entityLowerCase}Repository.DeleteAsync({Entity}Id);

                if (result)
                {
                    _logger.LogInformation("删除{Entity}成功");
                    Current{Entity} = null;
                    _original{Entity} = null;
                    {Entity}Id = Guid.Empty;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除{Entity}失败");
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 重新加载{Entity}数据
        /// </summary>
        public async Task ReloadAsync()
        {
            if ({Entity}Id != Guid.Empty)
            {
                await LoadExisting{Entity}Async();
                HasChanges = false;
            }
        }

        /// <summary>
        /// 标记数据已变更
        /// </summary>
        public void MarkAsChanged()
        {
            HasChanges = true;
        }

        #endregion 数据操作

        #region 辅助方法

        /// <summary>
        /// 转换为InputDto
        /// </summary>
        private {Entity}InputDto ConvertToInputDto({Entity}Dto {entityLowerCase})
        {
            return new {Entity}InputDto
            {
                Id = IsNew{Entity} ? null : {entityLowerCase}.Id,
                Name = {entityLowerCase}.Name,
                // TODO: 映射其他属性
            };
        }

        /// <summary>
        /// 克隆{Entity}对象（用于变更检测）
        /// </summary>
        private {Entity}Dto Clone{Entity}({Entity}Dto {entityLowerCase})
        {
            return new {Entity}Dto
            {
                Id = {entityLowerCase}.Id,
                Name = {entityLowerCase}.Name,
                // TODO: 映射其他属性
                CreatedAt = {entityLowerCase}.CreatedAt,
                UpdatedAt = {entityLowerCase}.UpdatedAt
            };
        }

        #endregion 辅助方法
    }
}
```

**关键设计要点**:
- ✅ **单一职责**: 仅负责数据管理，不涉及UI逻辑或命令处理
- ✅ **变更检测**: 通过`_original{Entity}`副本检测HasChanges
- ✅ **异步优先**: 所有I/O操作使用async/await
- ✅ **日志记录**: 关键操作记录日志，便于排查问题

---

## 3. CommandHandler代码模板

### 3.1 接口定义

**位置**: `LYBT.Desktop.Infrastructure/Interfaces/Components/ICommandHandler.cs`

```csharp
/// <summary>
/// 命令处理器接口 - 组件化MVVM架构核心接口
/// </summary>
public interface ICommandHandler
{
    /// <summary>执行命令</summary>
    Task<bool> ExecuteAsync(string commandName, object? parameter = null);

    /// <summary>检查命令是否可执行</summary>
    bool CanExecute(string commandName);
}
```

### 3.2 实现模板

**位置**: `LYBT.Desktop.{Module}/ViewModels/Components/{Entity}CommandHandler.cs`

```csharp
using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.{Module}.ViewModels.Components
{
    /// <summary>
    /// {Entity}命令处理器 - 组件化架构
    /// 职责单一：处理{Entity}相关的业务命令和事件发布
    /// Epic #1773 Task X: {Module}模块组件化改造
    /// </summary>
    public class {Entity}CommandHandler
    {
        #region 依赖注入

        private readonly ILogger<{Entity}CommandHandler> _logger;
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;
        private readonly IUserNotificationService? _userNotificationService;

        public {Entity}CommandHandler(
            ILogger<{Entity}CommandHandler> logger,
            IEventAggregator eventAggregator,
            IRegionManager regionManager,
            IUserNotificationService? userNotificationService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _userNotificationService = userNotificationService;

            // 初始化命令
            InitializeCommands();
        }

        #endregion 依赖注入

        #region 组件依赖

        private {Entity}DataManager? _dataManager;
        private {Entity}Validator? _validator;

        /// <summary>
        /// 设置组件依赖（从ViewModel调用）
        /// </summary>
        public void SetDependencies(
            {Entity}DataManager dataManager,
            {Entity}Validator validator)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));

            // 刷新命令CanExecute状态
            RefreshCommandStates();
        }

        #endregion 组件依赖

        #region 事件发布

        /// <summary>编辑启用事件</summary>
        public event Action? OnEditEnabled;

        /// <summary>编辑取消事件</summary>
        public event Action? OnEditCancelled;

        /// <summary>{Entity}保存事件</summary>
        public event Action? On{Entity}Saved;

        /// <summary>{Entity}删除事件</summary>
        public event Action? On{Entity}Deleted;

        #endregion 事件发布

        #region 命令定义

        /// <summary>保存命令</summary>
        public ICommand SaveCommand { get; private set; } = null!;

        /// <summary>编辑命令</summary>
        public ICommand EditCommand { get; private set; } = null!;

        /// <summary>删除命令</summary>
        public ICommand DeleteCommand { get; private set; } = null!;

        /// <summary>取消编辑命令</summary>
        public ICommand CancelEditCommand { get; private set; } = null!;

        /// <summary>返回命令</summary>
        public ICommand BackCommand { get; private set; } = null!;

        /// <summary>查看历史命令（可选）</summary>
        public ICommand ViewHistoryCommand { get; private set; } = null!;

        #endregion 命令定义

        #region 命令初始化

        private void InitializeCommands()
        {
            // 保存命令
            SaveCommand = new DelegateCommand(
                async () => await ExecuteSaveAsync(),
                CanSave)
                .ObservesProperty(() => _dataManager!.HasChanges);

            // 编辑命令
            EditCommand = new DelegateCommand(
                ExecuteEdit,
                CanEdit);

            // 删除命令
            DeleteCommand = new DelegateCommand(
                async () => await ExecuteDeleteAsync(),
                CanDelete);

            // 取消编辑命令
            CancelEditCommand = new DelegateCommand(
                ExecuteCancelEdit,
                CanCancelEdit);

            // 返回命令
            BackCommand = new DelegateCommand(
                ExecuteBack);

            // 查看历史命令（可选）
            ViewHistoryCommand = new DelegateCommand(
                ExecuteViewHistory,
                CanViewHistory);
        }

        #endregion 命令初始化

        #region 命令执行逻辑

        /// <summary>
        /// 执行保存命令
        /// </summary>
        private async Task ExecuteSaveAsync()
        {
            if (_dataManager == null || _validator == null)
            {
                _logger.LogWarning("组件依赖未设置，无法执行保存");
                return;
            }

            try
            {
                _logger.LogInformation("开始保存{Entity}");

                // 1. 数据验证
                if (_dataManager.Current{Entity} != null)
                {
                    var inputDto = _validator.ConvertToInputDto(_dataManager.Current{Entity});
                    var validationResult = await _validator.Validate{Entity}InputAsync(inputDto);

                    if (!_validator.IsValid(validationResult, out string errorMessage))
                    {
                        _logger.LogWarning("数据验证失败: {ErrorMessage}", errorMessage);
                        await ShowErrorMessageAsync($"数据验证失败: {errorMessage}");
                        return;
                    }
                }

                // 2. 保存数据
                var success = await _dataManager.SaveAsync();

                // 3. 发布事件
                if (success)
                {
                    _logger.LogInformation("{Entity}保存成功");
                    On{Entity}Saved?.Invoke();
                }
                else
                {
                    _logger.LogWarning("{Entity}保存失败");
                    await ShowErrorMessageAsync("保存失败：服务器未返回数据");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存{Entity}时发生异常");
                await ShowErrorMessageAsync($"保存失败: {ex.Message}");
            }
            finally
            {
                RefreshCommandStates();
            }
        }

        /// <summary>
        /// 检查是否可以保存
        /// </summary>
        private bool CanSave()
        {
            return _dataManager != null &&
                   _dataManager.HasChanges &&
                   !_dataManager.IsReadOnly;
        }

        /// <summary>
        /// 执行编辑命令
        /// </summary>
        private void ExecuteEdit()
        {
            if (_dataManager == null)
            {
                _logger.LogWarning("组件依赖未设置，无法执行编辑");
                return;
            }

            _logger.LogInformation("启用编辑模式");
            _dataManager.IsReadOnly = false;
            OnEditEnabled?.Invoke();
            RefreshCommandStates();
        }

        /// <summary>
        /// 检查是否可以编辑
        /// </summary>
        private bool CanEdit()
        {
            return _dataManager != null &&
                   _dataManager.IsReadOnly &&
                   !_dataManager.IsNew{Entity};
        }

        /// <summary>
        /// 执行删除命令
        /// </summary>
        private async Task ExecuteDeleteAsync()
        {
            if (_dataManager == null)
            {
                _logger.LogWarning("组件依赖未设置，无法执行删除");
                return;
            }

            try
            {
                // 确认删除
                var confirmed = await ShowConfirmAsync($"确认删除此{Entity}吗？");
                if (!confirmed)
                {
                    return;
                }

                _logger.LogInformation("开始删除{Entity}");

                var success = await _dataManager.DeleteAsync();

                if (success)
                {
                    _logger.LogInformation("{Entity}删除成功");
                    On{Entity}Deleted?.Invoke();
                }
                else
                {
                    _logger.LogWarning("{Entity}删除失败");
                    await ShowErrorMessageAsync("删除失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除{Entity}时发生异常");
                await ShowErrorMessageAsync($"删除失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查是否可以删除
        /// </summary>
        private bool CanDelete()
        {
            return _dataManager != null &&
                   !_dataManager.IsNew{Entity} &&
                   !_dataManager.IsReadOnly;
        }

        /// <summary>
        /// 执行取消编辑命令
        /// </summary>
        private async void ExecuteCancelEdit()
        {
            if (_dataManager == null)
            {
                _logger.LogWarning("组件依赖未设置，无法执行取消编辑");
                return;
            }

            _logger.LogInformation("取消编辑，重新加载数据");
            _dataManager.IsReadOnly = true;
            await _dataManager.ReloadAsync();
            OnEditCancelled?.Invoke();
            RefreshCommandStates();
        }

        /// <summary>
        /// 检查是否可以取消编辑
        /// </summary>
        private bool CanCancelEdit()
        {
            return _dataManager != null &&
                   !_dataManager.IsReadOnly &&
                   _dataManager.HasChanges;
        }

        /// <summary>
        /// 执行返回命令
        /// </summary>
        private void ExecuteBack()
        {
            _logger.LogInformation("返回到{Entity}列表");
            _regionManager.RequestNavigate("ContentRegion", "{Entity}ManagementView");
        }

        /// <summary>
        /// 执行查看历史命令（可选）
        /// </summary>
        private void ExecuteViewHistory()
        {
            if (_dataManager == null || _dataManager.{Entity}Id == Guid.Empty)
            {
                _logger.LogWarning("{Entity}ID为空，无法查看历史");
                return;
            }

            _logger.LogInformation("查看{Entity}历史");
            // TODO: 导航到历史记录视图
            _regionManager.RequestNavigate("ContentRegion", "{Entity}HistoryView",
                new NavigationParameters { { "{Entity}Id", _dataManager.{Entity}Id } });
        }

        /// <summary>
        /// 检查是否可以查看历史
        /// </summary>
        private bool CanViewHistory()
        {
            return _dataManager != null &&
                   _dataManager.{Entity}Id != Guid.Empty;
        }

        #endregion 命令执行逻辑

        #region 辅助方法

        /// <summary>
        /// 刷新命令状态
        /// </summary>
        private void RefreshCommandStates()
        {
            (SaveCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (EditCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (DeleteCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (CancelEditCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (ViewHistoryCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        private async Task ShowErrorMessageAsync(string message)
        {
            if (_userNotificationService != null)
            {
                await _userNotificationService.ShowErrorAsync(message);
            }
            else
            {
                _logger.LogError(message);
            }
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        private async Task<bool> ShowConfirmAsync(string message)
        {
            if (_userNotificationService != null)
            {
                return await _userNotificationService.ShowConfirmAsync(message);
            }
            else
            {
                _logger.LogWarning("确认对话框无法显示: {Message}", message);
                return false;
            }
        }

        #endregion 辅助方法
    }
}
```

**关键设计要点**:
- ✅ **命令模式**: 统一的命令执行接口（ExecuteAsync/CanExecute）
- ✅ **事件驱动**: 通过事件实现组件间通信
- ✅ **依赖注入**: 通过SetDependencies()设置DataManager和Validator
- ✅ **命令状态管理**: 使用ObservesProperty动态更新CanExecute

---

## 4. Validator代码模板

### 4.1 接口定义

**位置**: `LYBT.Desktop.Infrastructure/Interfaces/Components/IValidationService.cs`

```csharp
/// <summary>
/// 验证服务接口 - 组件化MVVM架构核心接口
/// </summary>
public interface IValidationService
{
    /// <summary>异步验证DTO对象</summary>
    Task<ValidationResult> ValidateAsync<T>(T dto) where T : class;

    /// <summary>同步验证DTO对象</summary>
    ValidationResult Validate<T>(T dto) where T : class;

    /// <summary>快速验证DTO对象（简化版本）</summary>
    bool IsValid<T>(T dto, out string errorMessage) where T : class;
}
```

### 4.2 实现模板

**位置**: `LYBT.Desktop.{Module}/ViewModels/Components/{Entity}Validator.cs`

```csharp
using FluentValidation;
using FluentValidation.Results;
using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Shared.Models.Contracts.{Module};
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.{Module}.ViewModels.Components
{
    /// <summary>
    /// {Entity}验证器 - 组件化架构
    /// 职责单一：集成FluentValidation，提供统一的验证接口
    /// Epic #1773 Task X: {Module}模块组件化改造
    /// </summary>
    public class {Entity}Validator
    {
        #region 依赖注入

        private readonly IValidationService _validationService;
        private readonly ILogger<{Entity}Validator> _logger;

        public {Entity}Validator(
            IValidationService validationService,
            ILogger<{Entity}Validator> logger)
        {
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion 依赖注入

        #region 验证方法

        /// <summary>
        /// 异步验证{Entity}InputDto
        /// </summary>
        /// <param name="inputDto">待验证的InputDto对象</param>
        /// <returns>FluentValidation验证结果</returns>
        public async Task<ValidationResult> Validate{Entity}InputAsync({Entity}InputDto inputDto)
        {
            if (inputDto == null)
            {
                _logger.LogWarning("InputDto为空，验证失败");
                return new ValidationResult(new[]
                {
                    new ValidationFailure(nameof(inputDto), "验证对象不能为空")
                });
            }

            try
            {
                _logger.LogDebug("开始验证{Entity}InputDto");

                // 使用ValidationService验证（自动从DI容器获取IValidator<{Entity}InputDto>）
                var result = await _validationService.ValidateAsync(inputDto);

                if (!result.IsValid)
                {
                    _logger.LogWarning("{Entity}验证失败，错误数: {ErrorCount}", result.Errors.Count);
                    foreach (var error in result.Errors)
                    {
                        _logger.LogDebug("验证错误: {PropertyName} - {ErrorMessage}",
                            error.PropertyName, error.ErrorMessage);
                    }
                }
                else
                {
                    _logger.LogDebug("{Entity}验证通过");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证{Entity}时发生异常");
                return new ValidationResult(new[]
                {
                    new ValidationFailure(nameof(inputDto), $"验证过程发生错误: {ex.Message}")
                });
            }
        }

        /// <summary>
        /// 快速验证（简化版本）
        /// </summary>
        /// <param name="result">FluentValidation验证结果</param>
        /// <param name="errorMessage">验证失败时的错误信息</param>
        /// <returns>验证是否通过</returns>
        public bool IsValid(ValidationResult result, out string errorMessage)
        {
            if (result.IsValid)
            {
                errorMessage = string.Empty;
                return true;
            }

            // 组合所有错误信息
            errorMessage = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
            _logger.LogDebug("验证失败: {ErrorMessage}", errorMessage);
            return false;
        }

        #endregion 验证方法

        #region 辅助方法

        /// <summary>
        /// 转换为InputDto（从Dto转换）
        /// </summary>
        public {Entity}InputDto ConvertToInputDto({Entity}Dto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            return new {Entity}InputDto
            {
                Id = dto.Id == Guid.Empty ? null : dto.Id,
                Name = dto.Name,
                // TODO: 映射其他属性
            };
        }

        #endregion 辅助方法
    }
}
```

**关键设计要点**:
- ✅ **统一验证入口**: 通过IValidationService统一验证
- ✅ **集成Shared.Validators**: 自动从DI容器获取IValidator<T>
- ✅ **错误信息组合**: 提供IsValid()方法组合多个错误信息
- ✅ **日志记录**: 验证失败时记录详细错误日志

---

## 5. ViewModel集成模式

### 5.1 标准集成模板

**位置**: `LYBT.Desktop.{Module}/ViewModels/{Entity}DetailViewModel.cs`

```csharp
using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.{Module}.ViewModels.Components;
using LYBT.Shared.Models.Contracts.{Module};
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.{Module}.ViewModels
{
    /// <summary>
    /// {Entity}详情视图模型 - 组件化架构
    /// Epic #1773 Task X: {Module}模块组件化改造
    /// 使用{Entity}DataManager、{Entity}CommandHandler、{Entity}Validator三个组件
    /// </summary>
    public class {Entity}DetailViewModel : UnifiedViewModelBase
    {
        #region 私有字段

        private readonly {Entity}DataManager _dataManager;
        private readonly {Entity}CommandHandler _commandHandler;
        private readonly {Entity}Validator _validator;

        #endregion 私有字段

        #region 属性（委托给组件）

        /// <summary>{Entity}ID</summary>
        public Guid {Entity}Id => _dataManager.{Entity}Id;

        /// <summary>当前{Entity}数据</summary>
        public {Entity}Dto? {Entity} => _dataManager.Current{Entity};

        /// <summary>是否正在加载</summary>
        public new bool IsLoading => _dataManager.IsLoading;

        /// <summary>是否只读模式</summary>
        public bool IsReadOnly
        {
            get => _dataManager.IsReadOnly;
            set
            {
                if (_dataManager.IsReadOnly != value)
                {
                    _dataManager.IsReadOnly = value;
                    RaisePropertyChanged();
                    RefreshCommands();
                }
            }
        }

        /// <summary>是否有未保存的变更</summary>
        public bool HasChanges => _dataManager.HasChanges;

        // TODO: 添加{Entity}特定的UI属性（从{Entity}Dto委托）
        public string {Entity}Name => {Entity}?.Name ?? string.Empty;
        public DateTime? CreatedAt => {Entity}?.CreatedAt;
        public DateTime? UpdatedAt => {Entity}?.UpdatedAt;

        #endregion 属性

        #region 命令（委托给CommandHandler）

        public ICommand LoadDataCommand => _commandHandler.BackCommand; // 使用返回命令
        public ICommand BackCommand => _commandHandler.BackCommand;
        public ICommand EditCommand => _commandHandler.EditCommand;
        public ICommand SaveCommand => _commandHandler.SaveCommand;
        public ICommand CancelEditCommand => _commandHandler.CancelEditCommand;
        public ICommand DeleteCommand => _commandHandler.DeleteCommand;
        public ICommand ViewHistoryCommand => _commandHandler.ViewHistoryCommand;

        #endregion 命令

        #region 构造函数

        public {Entity}DetailViewModel(
            {Entity}DataManager dataManager,
            {Entity}CommandHandler commandHandler,
            {Entity}Validator validator,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));

            // 设置组件依赖
            _commandHandler.SetDependencies(_dataManager, _validator);

            // 订阅组件事件
            _commandHandler.OnEditEnabled += HandleEditEnabled;
            _commandHandler.OnEditCancelled += HandleEditCancelled;
            _commandHandler.On{Entity}Saved += Handle{Entity}Saved;
            _commandHandler.On{Entity}Deleted += Handle{Entity}Deleted;
        }

        #endregion 构造函数

        #region 导航生命周期

        /// <summary>
        /// 处理导航参数（同步）- Issue #1240
        /// </summary>
        protected override void ProcessNavigationParameters(NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);

            // 立即设置导航参数，避免UI延迟
            if (parameters.ContainsKey("{Entity}Id"))
            {
                var {entityLowerCase}Id = parameters.GetValue<Guid>("{Entity}Id");

                if (parameters.ContainsKey("ViewMode"))
                {
                    var viewMode = parameters.GetValue<string>("ViewMode");
                    IsReadOnly = viewMode != "Edit";
                }

                // 在InitializeAsync中加载数据
            }
        }

        /// <summary>
        /// 异步初始化数据 - Issue #1240
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            // 在UI线程上异步加载数据
            if (parameters.ContainsKey("{Entity}Id"))
            {
                var {entityLowerCase}Id = parameters.GetValue<Guid>("{Entity}Id");
                await LoadDataAsync({entityLowerCase}Id);
            }
        }

        /// <inheritdoc/>
        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("{Entity}Id"))
            {
                var target{Entity}Id = navigationContext.Parameters.GetValue<Guid>("{Entity}Id");
                return {Entity}Id == target{Entity}Id;
            }

            return true;
        }

        /// <inheritdoc/>
        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);

            if (HasChanges)
            {
                // 可以在这里添加保存确认逻辑
            }
        }

        #endregion 导航生命周期

        #region 数据操作

        private async Task LoadDataAsync(Guid {entityLowerCase}Id)
        {
            try
            {
                await _dataManager.InitializeAsync({entityLowerCase}Id);
                RefreshProperties();
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"加载{Entity}详情失败: {ex.Message}");
            }
        }

        #endregion 数据操作

        #region 事件处理

        private void HandleEditEnabled()
        {
            IsReadOnly = false;
        }

        private async void HandleEditCancelled()
        {
            IsReadOnly = true;
            await _dataManager.ReloadAsync();
            RefreshProperties();
        }

        private async void Handle{Entity}Saved()
        {
            try
            {
                IsReadOnly = true;
                RefreshProperties();
                await ShowSuccessMessageAsync("{Entity}信息保存成功");
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"保存后处理失败: {ex.Message}");
            }
        }

        private async void Handle{Entity}Deleted()
        {
            try
            {
                await ShowSuccessMessageAsync("{Entity}删除成功");
                RegionManager.RequestNavigate("ContentRegion", "{Entity}ManagementView");
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"删除后处理失败: {ex.Message}");
            }
        }

        #endregion 事件处理

        #region 辅助方法

        /// <summary>
        /// 刷新所有属性
        /// </summary>
        private void RefreshProperties()
        {
            RaisePropertyChanged(nameof({Entity}Id));
            RaisePropertyChanged(nameof({Entity}));
            RaisePropertyChanged(nameof(IsLoading));
            RaisePropertyChanged(nameof({Entity}Name));
            RaisePropertyChanged(nameof(CreatedAt));
            RaisePropertyChanged(nameof(UpdatedAt));
            RaisePropertyChanged(nameof(HasChanges));
        }

        /// <summary>
        /// 刷新命令状态
        /// </summary>
        private new void RefreshCommands()
        {
            // 命令由CommandHandler管理，这里刷新CanExecute状态
            RefreshProperties();
        }

        #endregion 辅助方法
    }
}
```

**关键设计要点**:
- ✅ **属性委托**: ViewModel属性委托给DataManager
- ✅ **命令委托**: ViewModel命令委托给CommandHandler
- ✅ **事件订阅**: 订阅CommandHandler的事件
- ✅ **导航生命周期**: 在InitializeAsync中加载数据

---

## 6. DI注册模式

### 6.1 Module注册模板

**位置**: `LYBT.Desktop.{Module}/{Module}Module.cs`

```csharp
using LYBT.Desktop.{Module}.Interfaces;
using LYBT.Desktop.{Module}.Repositories;
using LYBT.Desktop.{Module}.ViewModels;
using LYBT.Desktop.{Module}.ViewModels.Components;
using LYBT.Desktop.{Module}.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;

namespace LYBT.Desktop.{Module}
{
    /// <summary>
    /// {Module}模块定义
    /// Epic #1773: 组件化架构注册
    /// </summary>
    public class {Module}Module : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // ========== Repository注册（Singleton） ==========
            containerRegistry.RegisterSingleton<I{Entity}Repository, {Entity}Repository>();

            // ========== 组件注册（Scoped生命周期） ==========
            // 注意：Prism使用Register()默认为Transient，需要手动管理Scoped生命周期
            // 在ViewModel中通过构造函数注入，每次导航创建新实例

            // DataManager组件
            containerRegistry.Register<{Entity}DataManager>();

            // CommandHandler组件
            containerRegistry.Register<{Entity}CommandHandler>();

            // Validator组件
            containerRegistry.Register<{Entity}Validator>();

            // ========== ViewModel注册（Scoped生命周期） ==========
            containerRegistry.Register<{Entity}DetailViewModel>();
            containerRegistry.Register<{Entity}ManagementViewModel>();

            // ========== View注册（用于导航） ==========
            containerRegistry.RegisterForNavigation<{Entity}DetailView, {Entity}DetailViewModel>();
            containerRegistry.RegisterForNavigation<{Entity}ManagementView, {Entity}ManagementViewModel>();
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化逻辑（可选）
        }
    }
}
```

### 6.2 InfrastructureModule注册ValidationService

**位置**: `LYBT.Desktop.Infrastructure/InfrastructureModule.cs`

```csharp
using FluentValidation;
using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Validators.{Module};  // 引用Shared.Validators
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Infrastructure
{
    public class InfrastructureModule : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // ========== ValidationService注册（Singleton） ==========
            containerRegistry.RegisterSingleton<IValidationService, ValidationService>();

            // ========== FluentValidation Validators注册 ==========
            // 方式1：手动注册单个Validator
            containerRegistry.Register<IValidator<{Entity}InputDto>, {Entity}InputDtoValidator>();

            // 方式2：自动扫描注册（推荐）
            // 注册Shared.Validators程序集中的所有Validators
            containerRegistry.RegisterValidatorsFromAssemblyContaining<{Entity}InputDtoValidator>();
        }
    }
}
```

**关键设计要点**:
- ✅ **Repository**: Singleton（全局单例）
- ✅ **Components**: Register（每次导航创建新实例）
- ✅ **ViewModel**: Register（每次导航创建新实例）
- ✅ **ValidationService**: Singleton（全局单例）
- ✅ **Validators**: 自动扫描Shared.Validators程序集

---

## 7. 常见问题解决

### 7.1 问题：组件依赖未设置

**症状**:
```
System.NullReferenceException: Object reference not set to an instance of an object.
at {Entity}CommandHandler.ExecuteSaveAsync()
```

**原因**: ViewModel未调用`_commandHandler.SetDependencies()`

**解决方案**:
```csharp
public {Entity}DetailViewModel(
    {Entity}DataManager dataManager,
    {Entity}CommandHandler commandHandler,
    {Entity}Validator validator,
    /* ... */)
{
    _dataManager = dataManager;
    _commandHandler = commandHandler;
    _validator = validator;

    // ✅ 必须设置组件依赖
    _commandHandler.SetDependencies(_dataManager, _validator);
}
```

---

### 7.2 问题：命令CanExecute未更新

**症状**: SaveCommand按钮始终禁用，即使HasChanges=true

**原因**: 命令未订阅属性变化

**解决方案**:
```csharp
// ✅ 使用ObservesProperty订阅属性变化
SaveCommand = new DelegateCommand(
    async () => await ExecuteSaveAsync(),
    CanSave)
    .ObservesProperty(() => _dataManager!.HasChanges);
    //  ^^^^^^^^^^ 订阅HasChanges属性变化
```

---

### 7.3 问题：Validator找不到IValidator<T>

**症状**:
```
ValidationService: 未找到类型 {Entity}InputDto 的Validator，跳过验证
```

**原因**: Shared.Validators中的Validator未注册到DI容器

**解决方案**:
```csharp
// 在InfrastructureModule.cs中注册
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // ✅ 自动扫描Shared.Validators程序集
    containerRegistry.RegisterValidatorsFromAssemblyContaining<PatientInputDtoValidator>();
}
```

---

### 7.4 问题：事件未触发

**症状**: OnPatientSaved事件触发，但ViewModel的HandlePatientSaved未执行

**原因**: ViewModel未订阅事件

**解决方案**:
```csharp
public {Entity}DetailViewModel(/* ... */)
{
    // ...

    // ✅ 必须订阅组件事件
    _commandHandler.On{Entity}Saved += Handle{Entity}Saved;
    _commandHandler.On{Entity}Deleted += Handle{Entity}Deleted;
}
```

---

### 7.5 问题：UI属性未刷新

**症状**: 保存成功后，UI显示的CreatedAt/UpdatedAt未更新

**原因**: ViewModel未调用RaisePropertyChanged()

**解决方案**:
```csharp
private async void Handle{Entity}Saved()
{
    IsReadOnly = true;

    // ✅ 必须刷新所有属性
    RefreshProperties();

    await ShowSuccessMessageAsync("{Entity}信息保存成功");
}

private void RefreshProperties()
{
    RaisePropertyChanged(nameof({Entity}));
    RaisePropertyChanged(nameof({Entity}Name));
    RaisePropertyChanged(nameof(CreatedAt));
    RaisePropertyChanged(nameof(UpdatedAt));
    RaisePropertyChanged(nameof(HasChanges));
}
```

---

### 7.6 问题：导航参数丢失

**症状**: 导航到{Entity}DetailView时，{Entity}Id为Guid.Empty

**原因**: 导航参数传递错误

**解决方案**:
```csharp
// ✅ 正确的导航参数传递
_regionManager.RequestNavigate("ContentRegion", "{Entity}DetailView",
    new NavigationParameters
    {
        { "{Entity}Id", {entityLowerCase}.Id },
        { "ViewMode", "View" }  // 或 "Edit"
    });
```

---

### 7.7 问题：HasChanges始终为false

**症状**: 修改数据后，SaveCommand始终禁用

**原因**: DataManager未检测到变更

**解决方案**:
```csharp
// 方式1：在DataManager中监听属性变化（推荐）
public void MarkAsChanged()
{
    HasChanges = true;
}

// 方式2：在ViewModel中手动标记
private string _patientName;
public string PatientName
{
    get => _patientName;
    set
    {
        if (SetProperty(ref _patientName, value))
        {
            _dataManager.MarkAsChanged();  // ✅ 手动标记变更
        }
    }
}
```

---

## 📚 相关文档

- **Client端架构总览**: [docs/explanation/architecture/client/README.md](../explanation/architecture/client/README.md)
- **Infrastructure层设计**: [docs/explanation/architecture/client/infrastructure-layer-design.md](../explanation/architecture/client/infrastructure-layer-design.md)
- **Shared层Validators**: [docs/explanation/architecture/shared/README.md](../explanation/architecture/shared/README.md)
- **Epic #1773总览**: [GitHub Issue #1773](https://github.com/shouqitao/LYBTZYZS/issues/1773)

---

**文档版本**: v1.0
**最后更新**: 2025-11-03
**维护负责**: Client端开发组
