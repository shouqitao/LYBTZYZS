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

    // ============ 反射访问 internal Program.TryAcquireSingleInstance ============

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
