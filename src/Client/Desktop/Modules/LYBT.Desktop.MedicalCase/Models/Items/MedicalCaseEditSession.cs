using System;

namespace LYBT.Desktop.MedicalCase.Models.Items;

/// <summary>
/// 医案编辑会话持有者（模块内单例）。
/// 持有唯一的 <see cref="MedicalCaseEditContext"/> 实例，确保 Command/Lifecycle 服务共享同一编辑会话。
/// MedicalCaseEditContext 本身以 Transient 注入本持有者（构造时捕获单个实例）；
/// 会话生命周期：导航进入时 BeginEdit、离开/删除时 Clear。
/// </summary>
public class MedicalCaseEditSession
{
    /// <summary>共享的编辑上下文实例</summary>
    public MedicalCaseEditContext Context { get; }

    public MedicalCaseEditSession(MedicalCaseEditContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }
}
