using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Infrastructure.ViewModels.Base;

/// <summary>
/// 编辑器子 VM 基类（A-31-C5-4 收敛）
/// 统一 IsDirty 脏标记 + 生命周期（InitializeForNewCase/Validate/Reset）+ 上下文变更订阅复用（E3 修复）。
/// 子类以自身业务名公开 Context（Patient/Herb/Formula）供 XAML 绑定；
/// InitializeFromDto/GetXData 为各模块字段映射，保留在子类（只提取高同构部分）。
/// User 结构差异（[ObservableProperty] + 缓存联动）保留现状注明例外。
/// </summary>
public abstract partial class EditorViewModelBase<TContext> : ObservableObject, IDisposable
    where TContext : ValidatableModelBase
{
    /// <summary>编辑上下文（子类以业务名公开，如 Patient/Herb/Formula）</summary>
    protected abstract TContext Context { get; set; }
    /// <summary>释放上下文订阅（P1-8：订阅需对称退订）。</summary>
    public void Dispose() => UnsubscribeContext();

    /// <summary>是否已修改（脏数据标记）</summary>
    public bool IsDirty { get; protected set; }

    /// <summary>新建编辑上下文</summary>
    protected abstract TContext CreateNewContext();

    /// <summary>初始化为新实体（新建场景）</summary>
    public virtual void InitializeForNewCase()
    {
        Context = CreateNewContext();
        IsDirty = false;
        SubscribeContext();
    }

    /// <summary>验证编辑内容</summary>
    public bool Validate() => Context.ValidateAll();

    /// <summary>重置编辑状态</summary>
    public virtual void Reset()
    {
        UnsubscribeContext();
        Context = CreateNewContext();
        IsDirty = false;
    }

    /// <summary>订阅上下文变更（-= 先于 +=，保证单次订阅，修复 E3 多次 Initialize 重复订阅）</summary>
    protected void SubscribeContext()
    {
        Context.PropertyChanged -= OnContextPropertyChanged;
        Context.PropertyChanged += OnContextPropertyChanged;
    }

    protected void UnsubscribeContext() => Context.PropertyChanged -= OnContextPropertyChanged;

    private void OnContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        IsDirty = true;
    }
}
