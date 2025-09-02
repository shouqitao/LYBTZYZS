using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Prescriptions.Interfaces;

/// <summary>
/// 处方模块接口 - UltraThink双层架构简化版
/// 职责：统一模块入口，纯委托模式
/// </summary>
public interface IPrescriptionsModule : IPrescriptionService
{
    // UltraThink双层架构：所有方法委托给对应的Query/Business服务
    // 简单诊所版本：移除复杂的事件系统、统计功能、批量操作等过度设计
}