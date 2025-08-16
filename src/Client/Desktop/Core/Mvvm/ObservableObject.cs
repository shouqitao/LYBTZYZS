using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LYBT.Desktop.Core.Mvvm
{
    /// <summary>
    /// 可观察对象基类 - 提供高性能的属性通知机制
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
        protected readonly Dictionary<string, object?> _propertyValues = new();
        
        /// <summary>
        /// 属性更改跟踪
        /// </summary>
        private readonly HashSet<string> _changedProperties = new();
        
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
        private readonly HashSet<string> _batchChangedProperties = new();
        
        protected ObservableObject()
        {
            _synchronizationContext = SynchronizationContext.Current;
        }
        
        /// <summary>
        /// 设置属性值（带自动通知）
        /// </summary>
        protected virtual bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            // 比较是否相等
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }
            
            // 触发属性即将更改事件
            OnPropertyChanging(propertyName);
            
            // 更新值
            field = value;
            
            // 记录更改
            if (propertyName != null)
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
        /// </summary>
        protected virtual bool SetPropertyValue<T>(
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (propertyName == null)
                return false;
            
            // 获取旧值
            _propertyValues.TryGetValue(propertyName, out var oldValue);
            
            // 比较是否相等
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
        /// </summary>
        protected virtual T GetPropertyValue<T>(
            T defaultValue = default!,
            [CallerMemberName] string? propertyName = null)
        {
            if (propertyName != null && _propertyValues.TryGetValue(propertyName, out var value))
            {
                return (T?)value ?? defaultValue;
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// 触发属性更改事件
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (_isBatchUpdating > 0)
            {
                // 批量更新模式，暂存更改
                if (propertyName != null)
                {
                    _batchChangedProperties.Add(propertyName);
                }
                return;
            }
            
            var handler = PropertyChanged;
            if (handler != null && propertyName != null)
            {
                // 确保在UI线程触发
                if (_synchronizationContext != null && _synchronizationContext != SynchronizationContext.Current)
                {
                    _synchronizationContext.Post(_ =>
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
        /// </summary>
        protected virtual void OnPropertyChanging([CallerMemberName] string? propertyName = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }
        
        /// <summary>
        /// 批量更新（减少通知次数）
        /// </summary>
        public IDisposable BeginBatchUpdate()
        {
            Interlocked.Increment(ref _isBatchUpdating);
            return new BatchUpdateScope(this);
        }
        
        /// <summary>
        /// 结束批量更新
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
        /// </summary>
        protected void RefreshAllProperties()
        {
            OnPropertyChanged(string.Empty);
        }
        
        /// <summary>
        /// 检查属性是否已更改
        /// </summary>
        public bool HasPropertyChanged(string propertyName)
        {
            return _changedProperties.Contains(propertyName);
        }
        
        /// <summary>
        /// 获取所有已更改的属性
        /// </summary>
        public IEnumerable<string> GetChangedProperties()
        {
            return _changedProperties;
        }
        
        /// <summary>
        /// 重置更改跟踪
        /// </summary>
        public void ResetChangeTracking()
        {
            _changedProperties.Clear();
        }
        
        /// <summary>
        /// 是否有任何更改
        /// </summary>
        public bool IsDirty => _changedProperties.Count > 0;
        
        /// <summary>
        /// 批量更新作用域
        /// </summary>
        private class BatchUpdateScope : IDisposable
        {
            private readonly ObservableObject _owner;
            
            public BatchUpdateScope(ObservableObject owner)
            {
                _owner = owner;
            }
            
            public void Dispose()
            {
                _owner.EndBatchUpdate();
            }
        }
    }
    
    /// <summary>
    /// 带验证的可观察对象
    /// </summary>
    public abstract class ValidatableObservableObject : ObservableObject, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> _errors = new();
        
        /// <summary>
        /// 错误变更事件
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        
        /// <summary>
        /// 是否有验证错误
        /// </summary>
        public bool HasErrors => _errors.Count > 0;
        
        /// <summary>
        /// 获取属性的错误信息
        /// </summary>
        public System.Collections.IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                // 返回所有错误
                var allErrors = new List<string>();
                foreach (var errors in _errors.Values)
                {
                    allErrors.AddRange(errors);
                }
                return allErrors;
            }
            
            return _errors.TryGetValue(propertyName, out var propertyErrors) 
                ? propertyErrors 
                : Array.Empty<string>();
        }
        
        /// <summary>
        /// 添加错误
        /// </summary>
        protected void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
            {
                _errors[propertyName] = new List<string>();
            }
            
            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                OnErrorsChanged(propertyName);
            }
        }
        
        /// <summary>
        /// 清除属性错误
        /// </summary>
        protected void ClearErrors(string propertyName)
        {
            if (_errors.Remove(propertyName))
            {
                OnErrorsChanged(propertyName);
            }
        }
        
        /// <summary>
        /// 清除所有错误
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
        protected virtual void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(HasErrors));
        }
        
        /// <summary>
        /// 验证属性
        /// </summary>
        protected abstract bool ValidateProperty(string propertyName, object? value);
        
        /// <summary>
        /// 验证所有属性
        /// </summary>
        public virtual bool Validate()
        {
            ClearAllErrors();
            
            // 验证所有缓存的属性
            foreach (var kvp in _propertyValues)
            {
                ValidateProperty(kvp.Key, kvp.Value);
            }
            
            return !HasErrors;
        }
        
        /// <summary>
        /// 重写SetProperty以包含验证
        /// </summary>
        protected override bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            // 先验证
            if (propertyName != null)
            {
                ClearErrors(propertyName);
                ValidateProperty(propertyName, value);
            }
            
            return base.SetProperty(ref field, value, propertyName);
        }
    }
}