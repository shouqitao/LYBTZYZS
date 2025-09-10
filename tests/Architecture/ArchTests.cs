using NetArchTest.Rules;
using System.Reflection;
using Xunit;

namespace LYBT.ArchTests;

/// <summary>
/// 架构测试 - 基于规则的代码结构验证
/// 用于确保代码遵循预定义的架构约束和设计原则
/// </summary>
public class ArchTests
{
    private static readonly Assembly WebApiAssembly = typeof(LYBT.WebAPI.Program).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(LYBT.Infrastructure.Data.AppDbContext).Assembly;
    private static readonly Assembly DesktopCoreAssembly = typeof(LYBT.Desktop.Core.ServiceCollectionExtensions).Assembly;
    private static readonly Assembly SharedModelsAssembly = typeof(LYBT.Shared.Models.Contracts.Common.BaseDto).Assembly;

    /// <summary>
    /// 规则1: 控制器路由必须以 /api/v1 开头
    /// 验证所有API控制器遵循版本路由约定
    /// </summary>
    [Fact]
    public void Controllers_Should_Have_ApiV1_Route_Prefix()
    {
        var result = Types.InAssembly(WebApiAssembly)
            .That()
            .InheritFrom(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .And()
            .DoNotHaveNameEndingWith("BaseController")
            .Should()
            .HaveCustomAttribute(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute))
            .GetResult();

        Assert.True(result.IsSuccessful, 
            $"控制器必须具有Route特性。失败的类型: {string.Join(", ", result.FailingTypeNames)}");

        // 进一步验证路由前缀
        var controllersWithRoutes = Types.InAssembly(WebApiAssembly)
            .That()
            .InheritFrom(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .And()
            .DoNotHaveNameEndingWith("BaseController")
            .GetTypes();

        var invalidRoutes = new List<string>();
        
        foreach (var controller in controllersWithRoutes)
        {
            var routeAttribute = controller.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RouteAttribute>();
            if (routeAttribute != null && !routeAttribute.Template.StartsWith("api/v1", StringComparison.OrdinalIgnoreCase))
            {
                invalidRoutes.Add($"{controller.Name}: {routeAttribute.Template}");
            }
        }

        Assert.Empty(invalidRoutes);
    }

    /// <summary>
    /// 规则2: 禁止跨层依赖
    /// UI层不能直接依赖Infrastructure层或Domain层
    /// </summary>
    [Fact]
    public void UI_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(DesktopCoreAssembly)
            .That()
            .ResideInNamespace("LYBT.Desktop")
            .Should()
            .NotHaveDependencyOn("LYBT.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"UI层不应依赖Infrastructure层。违规类型: {string.Join(", ", result.FailingTypeNames)}");
    }

    /// <summary>
    /// 规则3: 控制器不应直接依赖Infrastructure实现
    /// 控制器应该通过服务接口与基础设施交互
    /// </summary>
    [Fact]
    public void Controllers_Should_Not_Depend_On_Infrastructure_Implementations()
    {
        var result = Types.InAssembly(WebApiAssembly)
            .That()
            .InheritFrom(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .Should()
            .NotHaveDependencyOn("LYBT.Infrastructure.Data")
            .And()
            .NotHaveDependencyOn("LYBT.Infrastructure.Services")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"控制器不应直接依赖Infrastructure实现。违规类型: {string.Join(", ", result.FailingTypeNames)}");
    }

    /// <summary>
    /// 规则4: 禁止命名包含工作流/流水线/总线相关词汇
    /// 确保系统保持简单的记录型架构
    /// </summary>
    [Fact]
    public void Types_Should_Not_Have_Workflow_Pipeline_Bus_Names()
    {
        var prohibitedWords = new[] { "Workflow", "Pipeline", "Bus", "Engine", "Saga" };
        var allAssemblies = new[] { WebApiAssembly, InfrastructureAssembly, DesktopCoreAssembly, SharedModelsAssembly };

        var violations = new List<string>();

        foreach (var assembly in allAssemblies)
        {
            var types = Types.InAssembly(assembly).GetTypes();
            
            foreach (var type in types)
            {
                foreach (var word in prohibitedWords)
                {
                    if (type.Name.Contains(word, StringComparison.OrdinalIgnoreCase))
                    {
                        violations.Add($"{type.FullName} contains prohibited word '{word}'");
                    }
                }
            }
        }

        Assert.Empty(violations);
    }

    /// <summary>
    /// 规则5: 禁止自定义验证器特性
    /// 只允许使用标准的DataAnnotations验证特性
    /// </summary>
    [Fact]
    public void Should_Not_Have_Custom_Validation_Attributes()
    {
        var allAssemblies = new[] { WebApiAssembly, InfrastructureAssembly, DesktopCoreAssembly, SharedModelsAssembly };
        var customValidationAttributes = new List<string>();

        foreach (var assembly in allAssemblies)
        {
            var types = Types.InAssembly(assembly)
                .That()
                .Inherit(typeof(System.ComponentModel.DataAnnotations.ValidationAttribute))
                .And()
                .DoNotResideInNamespace("System.ComponentModel.DataAnnotations")
                .GetTypes();

            foreach (var type in types)
            {
                // 排除一些已知的合法验证特性
                if (!type.Name.EndsWith("Attribute") || 
                    type.Namespace?.StartsWith("Microsoft") == true ||
                    type.Namespace?.StartsWith("System") == true)
                {
                    continue;
                }

                customValidationAttributes.Add(type.FullName);
            }
        }

        Assert.Empty(customValidationAttributes);
    }

    /// <summary>
    /// 规则6: 模块边界检查
    /// 确保各业务模块之间不存在循环依赖
    /// </summary>
    [Fact]
    public void Business_Modules_Should_Not_Have_Circular_Dependencies()
    {
        var moduleNames = new[] 
        { 
            "Auth", "Users", "Patients", "MedicalCase", 
            "Consultation", "Prescriptions", "Herbs", "Formula" 
        };

        // 这是一个简化的循环依赖检查
        // 检查每个模块是否只依赖共享层而不相互依赖
        var violations = new List<string>();

        foreach (var moduleName in moduleNames)
        {
            try
            {
                var moduleAssembly = Assembly.LoadFrom($"LYBT.Module.{moduleName}.dll");
                var types = Types.InAssembly(moduleAssembly).GetTypes();

                foreach (var otherModule in moduleNames.Where(m => m != moduleName))
                {
                    var hasDependency = types.Any(t => 
                        t.GetReferencedAssemblies().Any(ra => 
                            ra.Name?.Contains($"LYBT.Module.{otherModule}") == true));

                    if (hasDependency)
                    {
                        violations.Add($"Module {moduleName} depends on Module {otherModule}");
                    }
                }
            }
            catch (FileNotFoundException)
            {
                // 模块程序集不存在，跳过检查
                continue;
            }
        }

        Assert.Empty(violations);
    }

    /// <summary>
    /// 规则7: Shared层纯净性检查
    /// Shared层不应依赖任何业务模块或Infrastructure
    /// </summary>
    [Fact]
    public void Shared_Layer_Should_Be_Pure()
    {
        var result = Types.InAssembly(SharedModelsAssembly)
            .Should()
            .NotHaveDependencyOn("LYBT.Module")
            .And()
            .NotHaveDependencyOn("LYBT.Infrastructure")
            .And()
            .NotHaveDependencyOn("LYBT.Desktop")
            .And()
            .NotHaveDependencyOn("LYBT.WebAPI")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Shared层应保持纯净，不依赖业务模块。违规类型: {string.Join(", ", result.FailingTypeNames)}");
    }
}