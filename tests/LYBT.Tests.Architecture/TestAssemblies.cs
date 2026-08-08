using System.Reflection;

namespace LYBT.Tests.Architecture;

/// <summary>
/// 公共程序集清单 — 所有架构测试统一引用
/// 消除各测试文件中重复的 Assembly[] 定义
/// </summary>
public static class TestAssemblies
{
    /// <summary>
    /// Server 端程序集（WebAPI + Infrastructure + Entities + 8 个 Module）
    /// </summary>
    public static readonly Assembly[] Server =
    [
        Assembly.Load("LYBT.WebAPI"),
        Assembly.Load("LYBT.Infrastructure"),
        Assembly.Load("LYBT.Entities"),
        Assembly.Load("LYBT.Module.Auth"),
        Assembly.Load("LYBT.Module.Users"),
        Assembly.Load("LYBT.Module.Patients"),
        Assembly.Load("LYBT.Module.MedicalCases"),
        Assembly.Load("LYBT.Module.Herbs"),
        Assembly.Load("LYBT.Module.Formulas"),
        Assembly.Load("LYBT.Module.Reports"),
        Assembly.Load("LYBT.Module.Registrations")
    ];

    /// <summary>
    /// Desktop 端程序集（Core 层 + 业务模块 + Shell）
    /// </summary>
    public static readonly Assembly[] Desktop =
    [
        Assembly.Load("LYBT.Desktop.Contracts"),
        Assembly.Load("LYBT.Desktop.Foundation"),
        Assembly.Load("LYBT.Desktop.Infrastructure"),
        Assembly.Load("LYBT.Desktop.Shell"),
        Assembly.Load("LYBT.Desktop.Auth"),
        Assembly.Load("LYBT.Desktop.Users"),
        Assembly.Load("LYBT.Desktop.Patients"),
        Assembly.Load("LYBT.Desktop.MedicalCase"),
        Assembly.Load("LYBT.Desktop.Herbs"),
        Assembly.Load("LYBT.Desktop.Formula"),
        Assembly.Load("LYBT.Desktop.Admin"),
        Assembly.Load("LYBT.Desktop.Clinical"),
        Assembly.Load("LYBT.Desktop.Registrations"),
        Assembly.Load("LYBT.Desktop.Controls"),
        Assembly.Load("LYBT.Desktop.Printing")
    ];

    /// <summary>
    /// 全部程序集（Server + Desktop + Shared）
    /// </summary>
    public static readonly Assembly[] All =
    [
        .. Server,
        .. Desktop,
        Assembly.Load("LYBT.Shared.Models"),
        Assembly.Load("LYBT.Shared.Configuration"),
        Assembly.Load("LYBT.Shared.ExceptionHandling"),
        Assembly.Load("LYBT.Shared.Logging")
    ];
}
