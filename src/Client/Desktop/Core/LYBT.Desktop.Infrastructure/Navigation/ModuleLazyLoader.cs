using LYBT.Desktop.Foundation.Modules;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Navigation;

/// <summary>
/// 业务模块懒加载实现
/// </summary>
public class ModuleLazyLoader : IModuleLazyLoader
{
    private readonly IModuleLoadingService? _moduleLoadingService;
    private readonly ILogger<ModuleLazyLoader> _logger;

    /// <summary>
    /// 视图名 → 业务模块名映射
    /// </summary>
    // 映射按各 Module.RegisterTypes 中 RegisterForNavigation 的实际注册方对齐。
    // ClinicalModule 注册角色台与薄包装管理视图（View 在角色台，Control 在业务模块）。
    private static readonly Dictionary<string, string> ViewToModuleMap = new(StringComparer.Ordinal)
    {
        // ClinicalModule
        { ViewNames.ClinicalHome, "ClinicalModule" },
        { ViewNames.ReceptionistHome, "ClinicalModule" },
        { ViewNames.ClinicalWorkspace, "ClinicalModule" },
        { ViewNames.PatientSelection, "ClinicalModule" },
        { ViewNames.MedicalCaseWorkspace, "ClinicalModule" },
        { ViewNames.PatientManagement, "ClinicalModule" },
        { ViewNames.MedicalCaseManagement, "ClinicalModule" },
        { ViewNames.HerbManagement, "ClinicalModule" },
        { ViewNames.FormulaManagement, "ClinicalModule" },

        // AdminModule
        { ViewNames.UserManagement, "AdminModule" },
        { ViewNames.SystemSettings, "AdminModule" },

        // MedicalCaseModule
        { ViewNames.MedicalCaseMasterDetail, "MedicalCaseModule" },
        { ViewNames.AuditLog, "MedicalCaseModule" },

        // RegistrationModule
        { ViewNames.RegistrationList, "RegistrationModule" },

        // ReportsModule
        { ViewNames.ReportsHome, "ReportsModule" },

        // SysadminModule
        { ViewNames.LogLevelControl, "SysadminModule" },
        { ViewNames.Deployment, "SysadminModule" },
        { ViewNames.BackupManagement, "SysadminModule" },
        { ViewNames.SecurityAuditLog, "SysadminModule" },
    };

    public ModuleLazyLoader(
        IModuleLoadingService? moduleLoadingService,
        ILogger<ModuleLazyLoader> logger)
    {
        _moduleLoadingService = moduleLoadingService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 确保目标视图所属的业务模块已加载
    /// </summary>
    public async Task EnsureModuleLoadedAsync(string viewName)
    {
        if (_moduleLoadingService == null) return;
        if (!ViewToModuleMap.TryGetValue(viewName, out var moduleName)) return;
        if (_moduleLoadingService.IsModuleLoaded(moduleName)) return;

        try
        {
            _logger.LogDebug("懒加载业务模块: {ModuleName}（触发视图: {ViewName}）", moduleName, viewName);
            await _moduleLoadingService.LoadModuleAsync(moduleName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "懒加载模块 {ModuleName} 失败", moduleName);
        }
    }

    public async Task PreloadModulesAsync(UserRole role)
    {
        if (_moduleLoadingService == null) return;

        // ClinicalModule 已改 OnDemand：Doctor/Receptionist 主页视图由其注册，预加载保证首屏就绪
        var modulesToPreload = role switch
        {
            UserRole.Doctor => new[] { "ClinicalModule", "PatientsModule", "CatalogModule", "MedicalCaseModule" },
            UserRole.Receptionist => new[] { "ClinicalModule", "PatientsModule", "RegistrationModule" },
            UserRole.Admin => new[] { "UsersModule", "ReportsModule" },
            UserRole.SuperAdmin => new[] { "UsersModule", "ReportsModule", "SysadminModule" },
            _ => Array.Empty<string>()
        };

        foreach (var moduleName in modulesToPreload)
        {
            if (_moduleLoadingService.IsModuleLoaded(moduleName)) continue;

            try
            {
                _logger.LogDebug("预加载模块: {ModuleName}", moduleName);
                await _moduleLoadingService.LoadModuleAsync(moduleName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "预加载模块失败: {ModuleName}", moduleName);
            }
        }
    }
}
