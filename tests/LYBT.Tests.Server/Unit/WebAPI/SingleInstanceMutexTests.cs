using FluentAssertions;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// US-SHELL-024（P2-07）单实例 Mutex 单测：反射调用 Program.TryAcquireSingleInstance——
/// 验证「首次获取成功 / 二次获取失败（已有实例拒绝）」。Mutex 为进程级命名互斥，
/// 测试用唯一名避免与真实部署/并行测试冲突。
/// </summary>
public class SingleInstanceMutexTests
{
    private static readonly string TestMutexName = $"LYBTZYZS_Test_Instance_{Guid.NewGuid():N}";

    [Fact]
    public void FirstAcquire_ReturnsTrue()
    {
        var result = Acquire(TestMutexName);
        result.Should().BeTrue("首次获取单实例锁应成功");
        Release();
    }

    [Fact]
    public void SecondAcquire_WhileHeld_ReturnsFalse()
    {
        // 第一个 Mutex 持有 → 第二个（不同实例同名称）应获取失败（已有实例在跑）
        var first = Acquire(TestMutexName + "_dual");
        first.Should().BeTrue();

        var second = Acquire(TestMutexName + "_dual");
        second.Should().BeFalse("已有实例持锁时二次获取必须失败（拒绝双开）");

        Release(); // 释放第二个（获取失败已 Dispose 内部 mutex——此处释放的是第一持锁）
    }

    [Fact]
    public void AfterRelease_AcquireAgain_ReturnsTrue()
    {
        var first = Acquire(TestMutexName + "_rel");
        first.Should().BeTrue();
        Release();

        var again = Acquire(TestMutexName + "_rel");
        again.Should().BeTrue("释放后可重新获取（旧进程退出 → 新进程可启动）");
        Release();
    }

    // ============ Mutex 基名可配置（多实例部署）============

    [Fact]
    public void ResolveInstanceMutexBaseName_Unset_ReturnsOriginalConstant()
    {
        // 未配置（null / 空 / 空白）必须回退原常量——零行为变更的守卫
        ResolveBaseName(null).Should().Be(DefaultInstanceMutexName,
            "未设 WebAPI__InstanceMutexName 时解析结果必须等于原常量（默认路径零行为变更）");
        ResolveBaseName(string.Empty).Should().Be(DefaultInstanceMutexName);
        ResolveBaseName("   ").Should().Be(DefaultInstanceMutexName);
    }

    [Fact]
    public void ResolveInstanceMutexBaseName_Configured_ReturnsConfiguredNameTrimmed()
    {
        ResolveBaseName(@"Global\LYBTZYZS_WebAPI_E2E_Instance").Should().Be(@"Global\LYBTZYZS_WebAPI_E2E_Instance");
        ResolveBaseName("  Global\\LYBTZYZS_WebAPI_E2E_Instance  ").Should().Be(@"Global\LYBTZYZS_WebAPI_E2E_Instance",
            "前后空白应被裁剪（避免因空白产生看似相同却不同的 mutex 名）");
    }

    [Fact]
    public void ResolveInstanceMutexBaseName_Configured_YieldsDistinctMutexNameForSameEnvironment()
    {
        // 多实例部署的核心断言：同环境下并存实例的 mutex 名必须不同，否则第二实例会被单实例保护拒绝。
        // 只做纯字符串断言（不实际获取固定名 mutex——本机若真在跑 WebAPI 会干扰，既有测试同样用唯一名避让）
        const string environment = "Production";
        var defaultName = $"{DefaultInstanceMutexName}_{environment}";
        var e2eName = $"{ResolveBaseName(@"Global\LYBTZYZS_WebAPI_E2E_Instance")}_{environment}";

        e2eName.Should().NotBe(defaultName);
        e2eName.Should().EndWith($"_{environment}", "环境后缀隔离保持不变（同机多环境仍互不干扰）");
    }

    // ============ 反射访问 internal Program.TryAcquireSingleInstance ============

    /// <summary>反射读取 Program.InstanceMutexName（原常量）。</summary>
    private static string DefaultInstanceMutexName =>
        (string)typeof(Program)
            .GetField("InstanceMutexName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .GetValue(null)!;

    /// <summary>反射调用 Program.ResolveInstanceMutexBaseName(string?)（internal，多实例部署支持）。</summary>
    private static string ResolveBaseName(string? configuredName)
    {
        var method = typeof(Program).GetMethod(
            "ResolveInstanceMutexBaseName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            null,
            new[] { typeof(string) },
            null
        );
        method
            .Should()
            .NotBeNull("Program.ResolveInstanceMutexBaseName(string) 应存在（多实例部署支持）");
        return (string)method!.Invoke(null, new object?[] { configuredName })!;
    }

    private static bool Acquire(string name)
    {
        var method = typeof(Program).GetMethod(
            "TryAcquireSingleInstance",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            null,
            new[] { typeof(string) },
            null
        );
        method
            .Should()
            .NotBeNull("Program.TryAcquireSingleInstance(string) 应存在（US-SHELL-024）");
        return (bool)method!.Invoke(null, new object[] { name })!;
    }

    private static void Release()
    {
        // 释放 Program._instanceMutex（反射）——让 Mutex 归还可复测
        var field = typeof(Program).GetField(
            "_instanceMutex",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        );
        var mutex = (System.Threading.Mutex?)field?.GetValue(null);
        try
        {
            mutex?.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // 未持有（获取失败场景）——忽略
        }
        mutex?.Dispose();
        field?.SetValue(null, null);
    }
}
