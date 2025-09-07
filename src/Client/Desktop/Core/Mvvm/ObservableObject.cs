using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LYBT.Desktop.Core.Mvvm;

/// <summary>
/// 可观察对象基类 - 提供高性能的属性通知机制
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 支持批量更新、变更跟踪、UI线程安全等企业级功能
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged, INotifyPropertyChanging
{

    /// <summary>
    /// 属性更改事件
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 属性即将更改事件
    /// </summary>
    public event PropertyChangingEventHandler? PropertyChanging;

    /// <summary>
    /// 属性值缓存，用于脏检查
    /// </summary>
    protected readonly Dictionary<string, object?> _propertyValues = [];

    /// <summary>
    /// 属性更改跟踪
    /// </summary>
    private readonly HashSet<string> _changedProperties = [];

    /// <summary>
    /// 同步上下文，确保UI线程更新
    /// </summary>
    private readonly SynchronizationContext? _synchronizationContext;

    /// <summary>
    /// 是否正在批量更新
    /// </summary>
    private int _isBatchUpdating;

    /// <summary>
    /// 批量更新期间的更改属性
    /// </summary>
    private readonly HashSet<string> _batchChangedProperties = [];

    protected ObservableObject()
    {
        _synchronizationContext = SynchronizationContext.Current;
    }

    /// <summary>
    /// 设置属性值（带自动通知）
    /// 使用现代化的相等性比较和空值检查
    /// </summary>
    /// <typeparam name="T">属性类型</typeparam>
    /// <param name="field">属性字段引用</param>
    /// <param name="value">新值</param>
    /// <param name="propertyName">属性名称（自动获取）</param>
    /// <returns>如果值发生更改则返回 true；否则返回 false</returns>
    protected virtual bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        // 使用现代化的相等性比较
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        // 触发属性即将更改事件
        OnPropertyChanging(propertyName);

        // 更新值
        field = value;

        // 记录更改（现代化的空值检查）
        if (!string.IsNullOrEmpty(propertyName))
        {
            _changedProperties.Add(propertyName);
            _propertyValues[propertyName] = value;
        }

        // 触发属性已更改事件
        OnPropertyChanged(propertyName);

        return true;
    }

    /// <summary>
    /// 设置属性值（使用缓存字典）
    /// 适用于使用字典存储属性值的场景
    /// </summary>
    /// <typeparam name="T">属性类型</typeparam>
    /// <param name="value">新值</param>
    /// <param name="propertyName">属性名称（自动获取）</param>
    /// <returns>如果值发生更改则返回 true；否则返回 false</returns>
    protected virtual bool SetPropertyValue<T>(
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        // 现代化空值检查
        if (string.IsNullOrEmpty(propertyName))
        {
            return false;
        }

        // 获取旧值
        _propertyValues.TryGetValue(propertyName, out var oldValue);

        // 现代化相等性比较
        if (EqualityComparer<T>.Default.Equals((T?)oldValue, value))
        {
            return false;
        }

        // 触发属性即将更改事件
        OnPropertyChanging(propertyName);

        // 更新值
        _propertyValues[propertyName] = value;
        _changedProperties.Add(propertyName);

        // 触发属性已更改事件
        OnPropertyChanged(propertyName);

        return true;
    }

    /// <summary>
    /// 获取属性值（从缓存字典）
    /// 使用现代化的空值处理和类型转换
    /// </summary>
    /// <typeparam name="T">属性类型</typeparam>
    /// <param name="defaultValue">默认值</param>
    /// <param name="propertyName">属性名称（自动获取）</param>
    /// <returns>属性值或默认值</returns>
    protected virtual T GetPropertyValue<T>(
        T defaultValue = default!,
        [CallerMemberName] string? propertyName = null)
    {
        // 现代化的空值检查和字典查找
        if (!string.IsNullOrEmpty(propertyName) &&
            _propertyValues.TryGetValue(propertyName, out var value))
        {
            return (T?)value ?? defaultValue;
        }

        return defaultValue;
    }

    /// <summary>
    /// 触发属性更改事件
    /// 支持UI线程安全和批量更新模式
    /// </summary>
    /// <param name="propertyName">属性名称（自动获取）</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (_isBatchUpdating > 0)
        {
            // 批量更新模式，暂存更改
            if (!string.IsNullOrEmpty(propertyName))
            {
                _batchChangedProperties.Add(propertyName);
            }
            return;
        }

        var handler = PropertyChanged;
        if (handler != null && !string.IsNullOrEmpty(propertyName))
        {
            // 确保在UI线程触发
            if (_synchronizationContext != null &&
                _synchronizationContext != SynchronizationContext.Current)
            {
                _synchronizationContext.Post(
                    _ =>
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                }, null);
            }
            else
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    /// <summary>
    /// 触发属性即将更改事件
    /// 在属性值实际更改之前通知订阅者
    /// </summary>
    /// <param name="propertyName">属性名称（自动获取）</param>
    protected virtual void OnPropertyChanging([CallerMemberName] string? propertyName = null)
    {
        PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
    }

    /// <summary>
    /// 开始批量更新（减少通知次数）
    /// 在大量属性更改时提升性能
    /// </summary>
    /// <returns>批量更新作用域，使用using语句自动结束</returns>
    public IDisposable BeginBatchUpdate()
    {
        Interlocked.Increment(ref _isBatchUpdating);
        return new BatchUpdateScope(this);
    }

    /// <summary>
    /// 结束批量更新
    /// 触发所有累积的属性更改通知
    /// </summary>
    private void EndBatchUpdate()
    {
        if (Interlocked.Decrement(ref _isBatchUpdating) == 0)
        {
            // 触发所有累积的更改
            foreach (var propertyName in _batchChangedProperties)
            {
                OnPropertyChanged(propertyName);
            }
            _batchChangedProperties.Clear();
        }
    }

    /// <summary>
    /// 刷新所有属性
    /// 触发所有绑定的UI更新
    /// </summary>
    protected void RefreshAllProperties()
    {
        OnPropertyChanged(string.Empty);
    }

    /// <summary>
    /// 检查属性是否已更改
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    /// <returns>如果属性已更改则返回 true；否则返回 false</returns>
    public bool HasPropertyChanged(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName, nameof(propertyName));
        return _changedProperties.Contains(propertyName);
    }

    /// <summary>
    /// 获取所有已更改的属性
    /// </summary>
    /// <returns>已更改属性名称的集合</returns>
    public IEnumerable<string> GetChangedProperties()
    {
        return _changedProperties;
    }

    /// <summary>
    /// 重置更改跟踪
    /// 清除所有更改记录，将对象标记为未修改状态
    /// </summary>
    public void ResetChangeTracking()
    {
        _changedProperties.Clear();
    }

    /// <summary>
    /// 获取一个值，指示对象是否有未保存的更改
    /// </summary>
    /// <value>如果有更改则为 true；否则为 false</value>
    public bool IsDirty => _changedProperties.Count > 0;

    /// <summary>
    /// 批量更新作用域
    /// 使用RAII模式自动管理批量更新生命周期
    /// </summary>
    /// <param name="owner">拥有者对象</param>
    private sealed class BatchUpdateScope(ObservableObject owner) : IDisposable
    {
        private readonly ObservableObject _owner = owner ?? throw new ArgumentNullException(nameof(owner));

        public void Dispose()
        {
            _owner.EndBatchUpdate();
        }
    }
}

/// <summary>
/// 带验证的可观察对象
/// 提供数据验证和错误通知功能，符合WPF数据绑定验证标准
/// </summary>
public abstract class ValidatableObservableObject : ObservableObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = [];

    /// <summary>
    /// 错误变更事件
    /// </summary>
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <summary>
    /// 获取一个值，指示是否有验证错误
    /// </summary>
    /// <value>如果有错误则为 true；否则为 false</value>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// 获取指定属性的错误信息
    /// </summary>
    /// <param name="propertyName">属性名称，null表示获取所有错误</param>
    /// <returns>错误信息的集合</returns>
    public System.Collections.IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            // 返回所有错误 - 使用C# 12集合表达式优化
            var allErrors = new List<string>();
            foreach (var errors in _errors.Values)
            {
                allErrors.AddRange(errors);
            }
            return allErrors;
        }

        return _errors.TryGetValue(propertyName, out var propertyErrors)
            ? propertyErrors
            : [];
    }

    /// <summary>
    /// 为指定属性添加验证错误
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    /// <param name="error">错误信息</param>
    /// <exception cref="ArgumentException">当属性名称或错误信息为空时抛出</exception>
    protected void AddError(string propertyName, string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName, nameof(propertyName));
        ArgumentException.ThrowIfNullOrWhiteSpace(error, nameof(error));

        if (!_errors.ContainsKey(propertyName))
        {
            _errors[propertyName] = [];
        }

        if (!_errors[propertyName].Contains(error))
        {
            _errors[propertyName].Add(error);
            OnErrorsChanged(propertyName);
        }
    }

    /// <summary>
    /// 清除指定属性的所有验证错误
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    /// <exception cref="ArgumentException">当属性名称为空时抛出</exception>
    protected void ClearErrors(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName, nameof(propertyName));

        if (_errors.Remove(propertyName))
        {
            OnErrorsChanged(propertyName);
        }
    }

    /// <summary>
    /// 清除所有属性的验证错误
    /// </summary>
    protected void ClearAllErrors()
    {
        var properties = _errors.Keys.ToArray();
        _errors.Clear();

        foreach (var propertyName in properties)
        {
            OnErrorsChanged(propertyName);
        }
    }

    /// <summary>
    /// 触发错误变更事件
    /// </summary>
    /// <param name="propertyName">发生错误变更的属性名称</param>
    protected virtual void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
    }

    /// <summary>
    /// 验证指定属性的值
    /// 子类必须实现此方法以提供具体的验证逻辑
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    /// <param name="value">属性值</param>
    /// <returns>如果验证通过则返回 true；否则返回 false</returns>
    protected abstract bool ValidateProperty(string propertyName, object? value);

    /// <summary>
    /// 验证所有已缓存的属性
    /// </summary>
    /// <returns>如果所有属性都验证通过则返回 true；否则返回 false</returns>
    public virtual bool Validate()
    {
        ClearAllErrors();

        // 验证所有缓存的属性
        foreach (var (propertyName, value) in _propertyValues)
        {
            ValidateProperty(propertyName, value);
        }

        return !HasErrors;
    }

    /// <summary>
    /// 重写SetProperty以包含自动验证
    /// 在设置属性值时自动执行验证逻辑
    /// </summary>
    /// <typeparam name="T">属性类型</typeparam>
    /// <param name="field">属性字段引用</param>
    /// <param name="value">新值</param>
    /// <param name="propertyName">属性名称（自动获取）</param>
    /// <returns>如果值发生更改则返回 true；否则返回 false</returns>
    protected override bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        // 先清除旧错误并验证新值
        if (!string.IsNullOrEmpty(propertyName))
        {
            ClearErrors(propertyName);
            ValidateProperty(propertyName, value);
        }

        return base.SetProperty(ref field, value, propertyName);
    }
}
