using System;
using System.Collections.ObjectModel;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.MedicalCase.Models.Items;

/// <summary>
/// 医案编辑会话持有者（模块内单例）。
/// 持有唯一的 <see cref="MedicalCaseEditContext"/> 实例，确保 Command/Lifecycle 服务共享同一编辑会话。
/// MedicalCaseEditContext 本身以 Transient 注入本持有者（构造时捕获单个实例）；
/// 会话生命周期：导航进入时 BeginEdit、离开/删除时 Clear；登录成功时 Reset（D-6：清除上一用户遗留状态）。
/// </summary>
public class MedicalCaseEditSession : IDisposable
{
    private readonly ILoginCoordinator? _loginCoordinator;
    private bool _disposed;

    /// <summary>共享的编辑上下文实例</summary>
    public MedicalCaseEditContext Context { get; }

    public MedicalCaseEditSession(MedicalCaseEditContext context, ILoginCoordinator? loginCoordinator = null)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _loginCoordinator = loginCoordinator;

        // D-6: 登录成功时重置模块内可变 Singleton（订阅模式对齐 ShellEventCoordinator）
        if (_loginCoordinator != null)
        {
            _loginCoordinator.LoginSucceeded += OnLoginSucceeded;
        }
    }

    /// <summary>重置编辑会话：清空可编辑状态与底层模型引用（登录切换用户时调用）</summary>
    public void Reset()
    {
        Context.PresentIllness = null;
        Context.TongueDiagnosis = null;
        Context.PulseDiagnosis = null;
        Context.TcmDiagnosis = null;
        Context.Remark = null;
        Context.Status = MedicalCaseStatus.Suspended;
        Context.PrescriptionItems = new ObservableCollection<PrescriptionItemModel>();
        Context.Clear();
    }

    private void OnLoginSucceeded(object? sender, LoginSuccessEventArgs e) => Reset();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_loginCoordinator != null)
        {
            _loginCoordinator.LoginSucceeded -= OnLoginSucceeded;
        }

        GC.SuppressFinalize(this);
    }
}
