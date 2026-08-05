using FluentAssertions;
using LYBT.Infrastructure.Configuration.Stores;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// JsonFileConfigurationStore 单元测试（临时文件，不依赖 DB）
/// </summary>
public class JsonFileConfigurationStoreTests : IDisposable
{
    private readonly string _tempDir;

    public JsonFileConfigurationStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "lybt-config-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string CreateStorePath() => Path.Combine(_tempDir, "runtime-overrides.json");

    [Fact]
    public async Task SetValue_ThenLoadAll_ReturnsSameValue()
    {
        // Arrange
        var store = new JsonFileConfigurationStore(CreateStorePath());

        // Act
        await store.SetValueAsync("Session:TimeoutMinutes", "60");

        // Assert
        var reloaded = new JsonFileConfigurationStore(CreateStorePath());
        var all = await reloaded.LoadAllAsync();
        all.Should().ContainKey("Session:TimeoutMinutes").WhoseValue.Should().Be("60");
    }

    [Fact]
    public async Task SetValue_EqualToBaseline_DoesNotPersistOverride()
    {
        // Arrange — baseline 中 Session:TimeoutMinutes=120，设置相同值不应落盘
        var baseline = new Dictionary<string, string?>
        {
            ["Session:TimeoutMinutes"] = "120"
        };
        var store = new JsonFileConfigurationStore(CreateStorePath(), baseline);

        // Act
        await store.SetValueAsync("Session:TimeoutMinutes", "120");

        // Assert — 覆盖为空，文件应不存在或内容为空
        File.Exists(CreateStorePath()).Should().BeFalse("与默认值相同的配置不应持久化");
    }

    [Fact]
    public async Task SetValue_DifferentFromBaseline_PersistsOverride()
    {
        // Arrange
        var baseline = new Dictionary<string, string?>
        {
            ["Session:TimeoutMinutes"] = "120"
        };
        var store = new JsonFileConfigurationStore(CreateStorePath(), baseline);

        // Act
        await store.SetValueAsync("Session:TimeoutMinutes", "60");

        // Assert
        var reloaded = new JsonFileConfigurationStore(CreateStorePath(), baseline);
        var all = await reloaded.LoadAllAsync();
        all.Should().ContainKey("Session:TimeoutMinutes").WhoseValue.Should().Be("60");
    }

    [Fact]
    public async Task Remove_ThenLoadAll_Empty()
    {
        // Arrange
        var store = new JsonFileConfigurationStore(CreateStorePath());
        await store.SetValueAsync("Session:TimeoutMinutes", "60");

        // Act
        await store.RemoveAsync("Session:TimeoutMinutes");

        // Assert
        var reloaded = new JsonFileConfigurationStore(CreateStorePath());
        var all = await reloaded.LoadAllAsync();
        all.Should().BeEmpty();
    }
}
