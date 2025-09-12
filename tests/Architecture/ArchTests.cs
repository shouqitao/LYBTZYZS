using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace LYBT.ArchTests;

/// <summary>
/// Record-Only 基线架构约束测试套件
/// 强制执行 Pass 7 治理基线中定义的所有架构规则
/// </summary>
public class ArchTests
{
    private static readonly Assembly[] Assemblies =
    [
        Assembly.Load("LYBT.WebAPI"),
        Assembly.Load("LYBT.Infrastructure"),
        Assembly.Load("LYBT.Entities"),
        Assembly.Load("LYBT.Shared.Models"),
        Assembly.Load("LYBT.Module.Auth"),
        Assembly.Load("LYBT.Module.Users"),
        Assembly.Load("LYBT.Module.Patients"),
        Assembly.Load("LYBT.Module.MedicalCase"),
        Assembly.Load("LYBT.Module.Consultation"),
        Assembly.Load("LYBT.Module.Prescriptions"),
        Assembly.Load("LYBT.Module.Herbs"),
        Assembly.Load("LYBT.Module.Formula")
    ];

    /// <summary>
    /// 层间依赖测试 - UI层不得直接依赖Infrastructure层
    /// </summary>
    [Fact]
    public void LayerDependencyTests_UI_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssemblies(Assemblies)
            .That()
            .ResideInNamespaceMatching(@".*\.ViewModels")
            .Or()
            .ResideInNamespaceMatching(@".*\.Controllers")
            .Should()
            .NotHaveDependencyOn("LYBT.Infrastructure")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"UI层违规依赖Infrastructure层: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? [])}");
    }

    /// <summary>
    /// 层间依赖测试 - UI层不得直接依赖Entities层
    /// </summary>
    [Fact]
    public void LayerDependencyTests_UI_Should_Not_Depend_On_Entities()
    {
        var result = Types.InAssemblies(Assemblies)
            .That()
            .ResideInNamespaceMatching(@".*\.ViewModels")
            .Or()
            .ResideInNamespaceMatching(@".*\.Controllers")
            .Should()
            .NotHaveDependencyOn("LYBT.Entities")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"UI层违规依赖Entities层: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? [])}");
    }

    /// <summary>
    /// API版本测试 - 所有控制器路由必须使用/api/v1前缀
    /// </summary>
    [Fact]
    public void ApiVersionTests_Controllers_Should_Use_V1_Routes_Only()
    {
        var controllers = Types.InAssemblies(Assemblies)
            .That()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .GetTypes();

        var violatingControllers = new List<string>();

        foreach (var controller in controllers)
        {
            var routeAttributes = controller.GetCustomAttributes()
                .Where(attr => attr.GetType().Name.Contains("Route"))
                .ToArray();

            foreach (var routeAttr in routeAttributes)
            {
                var template = routeAttr.GetType().GetProperty("Template")?.GetValue(routeAttr)?.ToString();
                if (!string.IsNullOrEmpty(template) &&
                    !template.StartsWith("api/v{version:apiVersion}/") &&
                    !template.StartsWith("api/v1/") &&
                    !template.Equals("api/v1/[controller]"))
                {
                    violatingControllers.Add($"{controller.Name}: {template}");
                }
            }
        }

        Assert.Empty(violatingControllers);
    }

    /// <summary>
    /// 控制器位置测试 - 所有控制器必须位于LYBT.WebAPI项目
    /// </summary>
    [Fact]
    public void ControllerLocationTests_All_Controllers_Should_Be_In_WebAPI_Project()
    {
        var allControllers = Types.InAssemblies(Assemblies)
            .That()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .GetTypes();

        var controllersOutsideWebAPI = allControllers
            .Where(t => !t.Assembly.GetName().Name?.Equals("LYBT.WebAPI", StringComparison.OrdinalIgnoreCase) == true)
            .Select(t => $"{t.Assembly.GetName().Name}.{t.Name}")
            .ToList();

        Assert.Empty(controllersOutsideWebAPI);
    }

    /// <summary>
    /// 命名规范测试 - 禁止使用Pipeline相关命名
    /// </summary>
    [Fact]
    public void NamingConventionTests_Should_Not_Contain_Pipeline_Names()
    {
        var prohibitedNames = new[] { "Pipeline", "Workflow", "Bus", "Engine", "Saga" };

        var violatingTypes = new List<string>();

        foreach (var prohibitedName in prohibitedNames)
        {
            var types = Types.InAssemblies(Assemblies)
                .That()
                .HaveNameMatching($".*{prohibitedName}.*")
                .GetTypes();

            violatingTypes.AddRange(types.Select(t => $"{t.FullName} (contains '{prohibitedName}')"));
        }

        Assert.Empty(violatingTypes);
    }

    /// <summary>
    /// 命名规范测试 - 禁止使用Workflow相关命名空间
    /// </summary>
    [Fact]
    public void NamingConventionTests_Should_Not_Have_Workflow_Namespaces()
    {
        var prohibitedNamespaces = new[] { "Workflows", "Pipelines", "Events", "Commands" };

        var violatingTypes = new List<string>();

        foreach (var prohibitedNs in prohibitedNamespaces)
        {
            var types = Types.InAssemblies(Assemblies)
                .That()
                .ResideInNamespaceMatching($@".*\.{prohibitedNs}\..*")
                .GetTypes();

            violatingTypes.AddRange(types.Select(t => $"{t.FullName} (namespace contains '{prohibitedNs}')"));
        }

        Assert.Empty(violatingTypes);
    }

    /// <summary>
    /// 禁止框架测试 - 不得引用工作流引擎等重型框架
    /// </summary>
    [Fact]
    public void ForbiddenFrameworkTests_Should_Not_Reference_Workflow_Frameworks()
    {
        var prohibitedFrameworks = new[]
        {
            "WorkflowFoundation", "Elsa", "Hangfire", "Quartz",
            "MediatR", "NServiceBus", "MassTransit", "Rebus"
        };

        var violatingReferences = new List<string>();

        foreach (var assembly in Assemblies)
        {
            var referencedAssemblies = assembly.GetReferencedAssemblies();

            foreach (var reference in referencedAssemblies)
            {
                if (prohibitedFrameworks.Any(framework =>
                    reference.Name?.Contains(framework, StringComparison.OrdinalIgnoreCase) == true))
                {
                    violatingReferences.Add($"{assembly.GetName().Name} references {reference.Name}");
                }
            }
        }

        Assert.Empty(violatingReferences);
    }

    /// <summary>
    /// 禁止框架测试 - 不得引用规则引擎框架
    /// </summary>
    [Fact]
    public void ForbiddenFrameworkTests_Should_Not_Reference_Rules_Engines()
    {
        var prohibitedRulesFrameworks = new[]
        {
            "RulesEngine", "DecisionTables", "BusinessRules"
        };

        var violatingTypes = new List<string>();

        foreach (var framework in prohibitedRulesFrameworks)
        {
            var types = Types.InAssemblies(Assemblies)
                .That()
                .HaveDependencyOn(framework)
                .GetTypes();

            violatingTypes.AddRange(types.Select(t => $"{t.FullName} depends on {framework}"));
        }

        Assert.Empty(violatingTypes);
    }

    /// <summary>
    /// 事务模式测试 - 禁止使用复杂事务协调框架
    /// </summary>
    [Fact]
    public void TransactionPatternTests_Should_Not_Use_Complex_Transaction_Frameworks()
    {
        var prohibitedTransactionPatterns = new[]
        {
            "Saga", "TransactionCoordinator", "CompensatingTransaction",
            "DistributedTransaction", "TwoPhaseCommit"
        };

        var violatingTypes = new List<string>();

        foreach (var pattern in prohibitedTransactionPatterns)
        {
            var types = Types.InAssemblies(Assemblies)
                .That()
                .HaveNameMatching($".*{pattern}.*")
                .GetTypes();

            violatingTypes.AddRange(types.Select(t => $"{t.FullName} (uses prohibited pattern '{pattern}')"));
        }

        Assert.Empty(violatingTypes);
    }

    /// <summary>
    /// Record-Only功能模式测试 - 禁止智能推荐相关类型
    /// </summary>
    [Fact]
    public void RecordOnlyTests_Should_Not_Have_Intelligence_Features()
    {
        var prohibitedIntelligenceFeatures = new[]
        {
            "Recommendation", "Intelligence", "AI", "MachineLearning",
            "Prediction", "Analytics", "SmartEngine"
        };

        var violatingTypes = new List<string>();

        foreach (var feature in prohibitedIntelligenceFeatures)
        {
            var types = Types.InAssemblies(Assemblies)
                .That()
                .HaveNameMatching($".*{feature}.*")
                .GetTypes();

            violatingTypes.AddRange(types.Select(t => $"{t.FullName} (contains prohibited feature '{feature}')"));
        }

        Assert.Empty(violatingTypes);
    }

    /// <summary>
    /// Record-Only功能模式测试 - 禁止复杂状态机
    /// </summary>
    [Fact]
    public void RecordOnlyTests_Should_Not_Have_Complex_State_Machines()
    {
        var prohibitedStateMachinePatterns = new[]
        {
            "StateMachine", "StateTransition", "ComplexState",
            "AutomatedWorkflow", "ProcessEngine"
        };

        var violatingTypes = new List<string>();

        foreach (var pattern in prohibitedStateMachinePatterns)
        {
            var types = Types.InAssemblies(Assemblies)
                .That()
                .HaveNameMatching($".*{pattern}.*")
                .GetTypes();

            violatingTypes.AddRange(types.Select(t => $"{t.FullName} (uses prohibited pattern '{pattern}')"));
        }

        Assert.Empty(violatingTypes);
    }

    /// <summary>
    /// 用户字段命名测试 - 用户相关字段必须使用Username命名
    /// </summary>
    [Fact]
    public void UserFieldNamingTests_Should_Use_Username_Convention()
    {
        var prohibitedUserFieldNames = new[]
        {
            "UserName", "user_name", "userName", "loginName"
        };

        var violatingProperties = new List<string>();

        foreach (var assembly in Assemblies)
        {
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                var properties = type.GetProperties();

                foreach (var property in properties)
                {
                    if (prohibitedUserFieldNames.Contains(property.Name))
                    {
                        violatingProperties.Add($"{type.FullName}.{property.Name}");
                    }
                }
            }
        }

        Assert.Empty(violatingProperties);
    }
}
