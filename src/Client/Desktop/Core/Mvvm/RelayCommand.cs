using System.Windows.Input;

namespace LYBT.Desktop.Core.Mvvm;

/// <summary>
/// 同步中继命令实现
/// 采用UltraThink架构标准，使用C# 12主构造函数和现代化特性
/// 提供简单而高效的命令模式实现，支持可执行状态动态判断
/// </summary>
/// <param name="execute">命令执行委托，不能为空</param>
/// <param name="canExecute">可执行状态判断委托，可选</param>
/// <exception cref="ArgumentNullException">当 <paramref name="execute"/> 为 null 时抛出</exception>
public class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Action _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Func<bool>? _canExecute = canExecute;

    /// <summary>
    /// 当命令的可执行状态可能发生更改时发生
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// 确定命令是否可以在其当前状态下执行
    /// </summary>
    /// <param name="parameter">命令使用的数据（此实现中忽略）</param>
    /// <returns>如果可以执行此命令，则为 true；否则为 false</returns>
    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    /// <summary>
    /// 执行命令逻辑
    /// </summary>
    /// <param name="parameter">命令使用的数据（此实现中忽略）</param>
    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            _execute();
        }
    }

    /// <summary>
    /// 手动触发CanExecuteChanged事件
    /// 用于通知UI更新命令的可执行状态
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// 带类型化参数的同步中继命令实现
/// 采用UltraThink架构标准，使用C# 12主构造函数和现代化特性
/// 提供类型安全的参数传递和执行控制
/// </summary>
/// <typeparam name="T">命令参数的类型</typeparam>
/// <param name="execute">命令执行委托，接收类型化参数</param>
/// <param name="canExecute">可执行状态判断委托，接收类型化参数</param>
/// <exception cref="ArgumentNullException">当 <paramref name="execute"/> 为 null 时抛出</exception>
public class RelayCommand<T>(Action<T?> execute, Predicate<T?>? canExecute = null) : ICommand
{
    private readonly Action<T?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Predicate<T?>? _canExecute = canExecute;

    /// <summary>
    /// 当命令的可执行状态可能发生更改时发生
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// 确定命令是否可以在其当前状态下执行
    /// </summary>
    /// <param name="parameter">命令使用的数据，将转换为类型 T</param>
    /// <returns>如果可以执行此命令，则为 true；否则为 false</returns>
    public bool CanExecute(object? parameter)
    {
        // 安全的类型转换和执行判断
        try
        {
            var typedParameter = (T?)parameter;
            return _canExecute?.Invoke(typedParameter) ?? true;
        }
        catch (InvalidCastException)
        {
            // 类型转换失败时，返回false表示无法执行
            return false;
        }
    }

    /// <summary>
    /// 执行命令逻辑
    /// </summary>
    /// <param name="parameter">命令使用的数据，将转换为类型 T</param>
    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            var typedParameter = (T?)parameter;
            _execute(typedParameter);
        }
    }

    /// <summary>
    /// 手动触发CanExecuteChanged事件
    /// 用于通知UI更新命令的可执行状态
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
