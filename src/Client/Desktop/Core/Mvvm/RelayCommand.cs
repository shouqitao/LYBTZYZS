using System;
using System.Windows.Input;

namespace LYBT.Desktop.Core.Mvvm
{
    /// <summary>
    /// 同步中继命令
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;
        
        /// <summary>
        /// CanExecute变更事件
        /// </summary>
        public event EventHandler? CanExecuteChanged;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        
        /// <summary>
        /// 是否可以执行
        /// </summary>
        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }
        
        /// <summary>
        /// 执行命令
        /// </summary>
        public void Execute(object? parameter)
        {
            _execute();
        }
        
        /// <summary>
        /// 触发CanExecuteChanged事件
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    
    /// <summary>
    /// 带参数的同步中继命令
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;
        
        /// <summary>
        /// CanExecute变更事件
        /// </summary>
        public event EventHandler? CanExecuteChanged;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        
        /// <summary>
        /// 是否可以执行
        /// </summary>
        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke((T?)parameter) ?? true;
        }
        
        /// <summary>
        /// 执行命令
        /// </summary>
        public void Execute(object? parameter)
        {
            _execute((T?)parameter);
        }
        
        /// <summary>
        /// 触发CanExecuteChanged事件
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}