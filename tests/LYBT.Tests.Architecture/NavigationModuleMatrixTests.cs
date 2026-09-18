using FluentAssertions;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Navigation;
using LYBT.Desktop.Infrastructure.Roles.Definitions;
using Xunit;

namespace LYBT.Tests.Architecture;

/// <summary>
/// N5 P1 模块矩阵守卫：角色 Home 所属模块必须在 RequiredModules
/// 设计：docs/compose/specs/desktop-navigation-viewmodel-design-2026-09-18.md §3.4 规则4
/// </summary>
public class NavigationModuleMatrixTests
{
    [Fact]
    public void RoleRequiredModules_ContainHomeViewModule()
    {
        var map = ModuleLazyLoader.ViewToModuleMap;
        var definitions = new IRoleDefinition[]
        {
            new DoctorRoleDefinition(),
            new ReceptionistRoleDefinition(),
            new AdminRoleDefinition(),
            new SuperAdminRoleDefinition(),
        };

        foreach (var def in definitions)
        {
            var home = def.HomeViewName;
            map.Should().ContainKey(home, $"角色 {def.Role} 的 HomeView '{home}' 必须在 ModuleLazyLoader.ViewToModuleMap 中");

            var module = map[home];
            def.RequiredModules.Should().Contain(module,
                $"角色 {def.Role} 的 Home 所属模块 '{module}' 必须在 RequiredModules（设计：角色 Home 所属模块必须预加载）");
        }
    }

    [Fact]
    public void ClinicalModule_CriticalViews_AreMapped()
    {
        var map = ModuleLazyLoader.ViewToModuleMap;
        var clinicalViews = new[]
        {
            ViewNames.ClinicalWorkspace,
            ViewNames.ReceptionistHome,
            ViewNames.PatientManagement,
            ViewNames.HerbManagement,
            ViewNames.FormulaManagement,
            ViewNames.MedicalCaseWorkspace,
        };

        foreach (var view in clinicalViews)
        {
            map.Should().ContainKey(view, $"ClinicalModule 关键视图 '{view}' 应在 ViewToModuleMap");
            map[view].Should().Be("ClinicalModule");
        }
    }

    [Fact]
    public void RoleDefinitions_RequiredModules_CoverManagementViews()
    {
        // SuperAdmin 管理矩阵：Herb/Formula/Patient → ClinicalModule；Reports → ReportsModule；AuditLog → MedicalCaseModule
        var superAdmin = new SuperAdminRoleDefinition();
        superAdmin.RequiredModules.Should().Contain("ClinicalModule");
        superAdmin.RequiredModules.Should().Contain("ReportsModule");
        superAdmin.RequiredModules.Should().Contain("MedicalCaseModule");
        superAdmin.RequiredModules.Should().Contain("SysadminModule");

        // Admin 可访问 Herb/Formula 管理（ClinicalModule 薄包装）+ Home=AdminModule
        var admin = new AdminRoleDefinition();
        admin.RequiredModules.Should().Contain("ClinicalModule");
        admin.RequiredModules.Should().Contain("AdminModule");

        // Doctor/Receptionist Home 在 ClinicalModule
        new DoctorRoleDefinition().RequiredModules.Should().Contain("ClinicalModule");
        new ReceptionistRoleDefinition().RequiredModules.Should().Contain("ClinicalModule");
    }
}
