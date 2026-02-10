using System.Reflection;
using NetArchTest.Rules;

namespace LYBT.Tests.Architecture;

/// <summary>
/// 聚合根模式架构测试（AR-001, AR-003）
/// Issue #1611 Phase 4
/// </summary>
public class AggregateRootArchTests
{
    private static readonly Assembly[] ServerAssemblies =
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
        Assembly.Load("LYBT.Module.Sync")
    ];

    /// <summary>
    /// AR-001: 聚合根模式验证 - MedicalCase作为聚合根
    /// OpenSpec: consolidate-medicalcase-queries - 已删除ConsultationController和PrescriptionsController
    /// 所有医案相关写操作统一通过MedicalCaseController
    /// </summary>
    [Fact]
    public void AR001_MedicalCase_Should_Be_Aggregate_Root()
    {
        // 1. 验证ConsultationController和PrescriptionsController已被删除（聚合根模式）
        var consultationController = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveName("ConsultationController")
            .GetTypes()
            .FirstOrDefault();

        Assert.Null(consultationController); // 应该已被删除

        var prescriptionsController = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveName("PrescriptionsController")
            .GetTypes()
            .FirstOrDefault();

        Assert.Null(prescriptionsController); // 应该已被删除

        // 2. 验证MedicalCaseController存在且包含Write方法（作为聚合根唯一入口）
        var medicalCaseController = Types.InAssemblies(ServerAssemblies)
            .That()
            .HaveName("MedicalCaseController")
            .GetTypes()
            .FirstOrDefault();

        Assert.NotNull(medicalCaseController);

        var medicalCaseMethods = medicalCaseController.GetMethods(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.DeclaredOnly);

        var medicalCaseWriteMethods = medicalCaseMethods.Where(m =>
            m.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpPostAttribute), false).Any() ||
            m.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpPutAttribute), false).Any()
        ).ToList();

        Assert.NotEmpty(medicalCaseWriteMethods);
    }

    /// <summary>
    /// AR-003: 软删除一致性验证
    /// 所有实体必须包含IsDeleted属性
    /// 例外：值对象、日志表、会话表、安全敏感表
    /// </summary>
    [Fact]
    public void AR003_All_Entities_Should_Support_Soft_Delete()
    {
        var entitiesAssembly = Assembly.Load("LYBT.Entities");

        // 合理的架构例外（值对象、日志、会话、安全）
        var softDeleteExceptions = new HashSet<string>
        {
            "LYBT.Entities.Users.AdminSecretModel",      // 安全敏感：密码哈希不应软删除
            "LYBT.Entities.Prescriptions.PrescriptionItem", // 值对象：通过Prescription管理
            "LYBT.Entities.Formula.FormulaHerbItem",     // 值对象：通过Formula管理
            "LYBT.Entities.Formulas.FormulaHerbItem",    // 值对象：通过Formula管理（新命名空间）
            "LYBT.Entities.Common.SystemLog",            // 日志表：只增不删，用于审计
            "LYBT.Entities.Auth.AuthSession",            // 会话表：过期自动清理
            "LYBT.Entities.Common.EntityAuditLog",       // 审计日志：只增不删，永久保留
            "LYBT.Entities.Auth.SecurityAuditLog",       // 安全审计日志：只增不删，永久保留
            "LYBT.Entities.MedicalCases.MedicalCaseAuditLog" // 医案审计日志：只增不删，永久保留
        };

        // 获取所有公共类（排除抽象类和静态类）
        var entityTypes = Types.InAssembly(entitiesAssembly)
            .That()
            .AreClasses()
            .And()
            .ArePublic()
            .And()
            .DoNotHaveNameMatching(".*Configuration")  // 排除EF配置类
            .And()
            .DoNotHaveNameMatching(".*Exception")      // 排除异常类
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsSealed)  // 排除抽象类和静态类
            .ToList();

        var entitiesWithoutSoftDelete = new List<string>();

        foreach (var entityType in entityTypes)
        {
            var fullName = entityType.FullName ?? entityType.Name;

            // 跳过白名单中的例外
            if (softDeleteExceptions.Contains(fullName))
                continue;

            // 检查直接包含IsDeleted属性
            var hasIsDeleted = entityType.GetProperty("IsDeleted",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.FlattenHierarchy) != null;

            // 检查是否继承自BaseEntity
            var inheritsFromBaseEntity = entityType.BaseType?.Name == "BaseEntity";

            if (!hasIsDeleted && !inheritsFromBaseEntity)
            {
                entitiesWithoutSoftDelete.Add(fullName);
            }
        }

        Assert.Empty(entitiesWithoutSoftDelete);
    }
}
