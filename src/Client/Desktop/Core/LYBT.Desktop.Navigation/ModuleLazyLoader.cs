using LYBT.Desktop.Foundation.Modules;
using LYBT.Desktop.Infrastructure.Constants;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Navigation;

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
    private static readonly Dictionary<string, string> ViewToModuleMap = new(StringComparer.Ordinal)
    {
        { ViewNames.PatientManagement, "PatientsModule" },
        { ViewNames.PatientSelection, "PatientsModule" },
        { ViewNames.ClinicalWorkspace, "PatientsModule" },
        { ViewNames.HerbManagement, "HerbsModule" },
        { ViewNames.FormulaManagement, "FormulaModule" },
        { ViewNames.UserManagement, "UsersModule" },
        { ViewNames.MedicalCaseManagement, "MedicalCaseModule" },
        { ViewNames.MedicalCaseWorkspace, "MedicalCaseModule" },
        { ViewNames.MedicalCaseMasterDetail, "MedicalCaseModule" },
        { ViewNames.RegistrationList, "RegistrationModule" },
        { ViewNames.ReportsHome, "ReportsModule" },
        { ViewNames.AuditLog, "MedicalCaseModule" },
        { ViewNames.SystemSettings, "AdminModule" },
        { ViewNames.LogLevelControl, "SysadminModule" },
        { ViewNames.Deployment, "SysadminModule" },
    };

    public ModuleLazyLoader(
        IModuleLoadingService? moduleLoadingService,
        ILogger<ModuleLazyLoader> logger)
    {
        _moduleLoadingService = moduleLoadingService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void EnsureModuleLoaded(string viewName)
    {
        if (_moduleLoadingService == null) return;
        if (!ViewToModuleMap.TryGetValue(viewName, out var moduleName)) return;
        if (_moduleLoadingService.IsModuleLoaded(moduleName)) return;

        try
        {
            _logger.LogDebug("懒加载业务模块: {ModuleName}（触发视图: {ViewName}）", moduleName, viewName);
            _moduleLoadingService.LoadModuleAsync(moduleName).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "懒加载模块 {ModuleName} 失败", moduleName);
        }
    }
}
