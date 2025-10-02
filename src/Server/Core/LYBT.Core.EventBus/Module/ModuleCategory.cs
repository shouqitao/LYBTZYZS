namespace LYBT.Core.EventBus.Module;

/// <summary>
/// 模块类别枚举
/// 用于对模块进行分类管理
/// </summary>
public enum ModuleCategory
{
    /// <summary>
    /// 核心模块
    /// 系统核心功能模块，不可禁用
    /// </summary>
    Core = 0,

    /// <summary>
    /// 基础设施模块
    /// 提供基础设施和通用功能的模块
    /// </summary>
    Infrastructure = 1,

    /// <summary>
    /// 业务模块
    /// 实现具体业务逻辑的模块
    /// </summary>
    Business = 2,

    /// <summary>
    /// 认证授权模块
    /// 处理用户认证和权限管理的模块
    /// </summary>
    Authentication = 3,

    /// <summary>
    /// 数据访问模块
    /// 数据库访问和数据处理相关模块
    /// </summary>
    DataAccess = 4,

    /// <summary>
    /// 外部集成模块
    /// 与外部系统集成的模块
    /// </summary>
    Integration = 5,

    /// <summary>
    /// 工具模块
    /// 提供辅助工具和实用功能的模块
    /// </summary>
    Utility = 6,

    /// <summary>
    /// 测试模块
    /// 仅用于测试环境的模块
    /// </summary>
    Testing = 7,

    /// <summary>
    /// 第三方模块
    /// 第三方开发的扩展模块
    /// </summary>
    ThirdParty = 8,

    /// <summary>
    /// 实验性模块
    /// 处于实验阶段的功能模块
    /// </summary>
    Experimental = 9
}

/// <summary>
/// 模块类别扩展方法
/// </summary>
public static class ModuleCategoryExtensions
{
    /// <summary>
    /// 获取类别的显示名称
    /// </summary>
    /// <param name="category">模块类别</param>
    /// <returns>显示名称</returns>
    public static string GetDisplayName(this ModuleCategory category)
    {
        return category switch
        {
            ModuleCategory.Core => "核心模块",
            ModuleCategory.Infrastructure => "基础设施",
            ModuleCategory.Business => "业务模块",
            ModuleCategory.Authentication => "认证授权",
            ModuleCategory.DataAccess => "数据访问",
            ModuleCategory.Integration => "外部集成",
            ModuleCategory.Utility => "工具模块",
            ModuleCategory.Testing => "测试模块",
            ModuleCategory.ThirdParty => "第三方模块",
            ModuleCategory.Experimental => "实验性模块",
            _ => category.ToString()
        };
    }

    /// <summary>
    /// 获取类别的描述
    /// </summary>
    /// <param name="category">模块类别</param>
    /// <returns>类别描述</returns>
    public static string GetDescription(this ModuleCategory category)
    {
        return category switch
        {
            ModuleCategory.Core => "系统核心功能模块，提供基础运行时支持",
            ModuleCategory.Infrastructure => "提供基础设施和通用功能支持",
            ModuleCategory.Business => "实现具体业务逻辑和功能",
            ModuleCategory.Authentication => "处理用户认证、授权和权限管理",
            ModuleCategory.DataAccess => "提供数据库访问和数据处理功能",
            ModuleCategory.Integration => "与外部系统和服务进行集成",
            ModuleCategory.Utility => "提供辅助工具和实用功能",
            ModuleCategory.Testing => "仅在测试环境中使用的功能模块",
            ModuleCategory.ThirdParty => "由第三方开发者提供的扩展模块",
            ModuleCategory.Experimental => "处于实验阶段的新功能模块",
            _ => "未定义类别"
        };
    }

    /// <summary>
    /// 检查是否为核心类别
    /// </summary>
    /// <param name="category">模块类别</param>
    /// <returns>是否为核心类别</returns>
    public static bool IsCore(this ModuleCategory category)
    {
        return category is ModuleCategory.Core or ModuleCategory.Infrastructure;
    }

    /// <summary>
    /// 检查是否为可选类别
    /// </summary>
    /// <param name="category">模块类别</param>
    /// <returns>是否为可选类别</returns>
    public static bool IsOptional(this ModuleCategory category)
    {
        return category is ModuleCategory.ThirdParty or
                          ModuleCategory.Experimental or
                          ModuleCategory.Testing;
    }

    /// <summary>
    /// 获取类别的默认优先级
    /// </summary>
    /// <param name="category">模块类别</param>
    /// <returns>默认优先级</returns>
    public static int GetDefaultPriority(this ModuleCategory category)
    {
        return category switch
        {
            ModuleCategory.Core => 10,
            ModuleCategory.Infrastructure => 20,
            ModuleCategory.Authentication => 30,
            ModuleCategory.DataAccess => 40,
            ModuleCategory.Business => 50,
            ModuleCategory.Integration => 60,
            ModuleCategory.Utility => 70,
            ModuleCategory.ThirdParty => 80,
            ModuleCategory.Testing => 90,
            ModuleCategory.Experimental => 100,
            _ => 50
        };
    }
}
