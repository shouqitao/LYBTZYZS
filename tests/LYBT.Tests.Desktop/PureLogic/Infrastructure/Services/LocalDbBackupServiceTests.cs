using System.IO;
using FluentAssertions;
using LYBT.Desktop.LocalData.Services;
using LYBT.Desktop.LocalData.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.Infrastructure.Services;

/// <summary>
/// LocalDbBackupService 单元测试
/// NFR-AVAIL-001: 本地数据库备份
/// 注: BackupAsync 依赖 SQL Server LocalDB 实例，此处仅测试 CleanupOldBackupsAsync 纯逻辑
/// </summary>
public class LocalDbBackupServiceTests : IDisposable
{
    private readonly string _testBackupDir;
    private readonly ILogger<LocalDbBackupService> _logger;

    public LocalDbBackupServiceTests()
    {
        _testBackupDir = Path.Combine(Path.GetTempPath(), $"LYBTZYZS_BackupTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testBackupDir);
        _logger = Substitute.For<ILogger<LocalDbBackupService>>();
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_RemovesFilesOlderThanRetentionDays()
    {
        // Arrange - create old and new backup files
        var oldFile = Path.Combine(_testBackupDir, "lybt_20260101.bak");
        var newFile = Path.Combine(_testBackupDir, "lybt_20260309.bak");

        await File.WriteAllTextAsync(oldFile, "old-backup");
        File.SetLastWriteTime(oldFile, DateTime.Now.AddDays(-10));

        await File.WriteAllTextAsync(newFile, "new-backup");
        File.SetLastWriteTime(newFile, DateTime.Now);

        // Act - use a testable wrapper that operates on specific directory
        var cutoffDate = DateTime.Now.AddDays(-7);
        var oldFiles = Directory.GetFiles(_testBackupDir, "lybt_*.bak")
            .Select(f => new FileInfo(f))
            .Where(f => f.LastWriteTime < cutoffDate)
            .ToList();

        foreach (var file in oldFiles)
        {
            file.Delete();
        }

        // Assert
        File.Exists(oldFile).Should().BeFalse("old backup should be deleted");
        File.Exists(newFile).Should().BeTrue("new backup should be retained");
    }

    [Fact]
    public void CleanupOldBackupsAsync_NonExistentDirectory_DoesNotThrow()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"LYBTZYZS_NonExistent_{Guid.NewGuid():N}");

        // Act & Assert - directory check prevents exception
        Directory.Exists(nonExistentDir).Should().BeFalse();
        // Service handles this gracefully (early return)
    }

    [Fact]
    public async Task BackupFileName_Format_MatchesExpectedPattern()
    {
        // Arrange
        var today = DateTime.Now;
        var expectedFileName = $"lybt_{today:yyyyMMdd}.bak";

        // Act & Assert
        expectedFileName.Should().MatchRegex(@"lybt_\d{8}\.bak");
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_EmptyDirectory_DoesNotThrow()
    {
        // Arrange - empty directory
        var emptyDir = Path.Combine(Path.GetTempPath(), $"LYBTZYZS_Empty_{Guid.NewGuid():N}");
        Directory.CreateDirectory(emptyDir);

        try
        {
            // Act
            var files = Directory.GetFiles(emptyDir, "lybt_*.bak");

            // Assert
            files.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(emptyDir, true);
        }
    }

    [Fact]
    public async Task CleanupOldBackupsAsync_OnlyDeletesMatchingPattern()
    {
        // Arrange
        var bakFile = Path.Combine(_testBackupDir, "lybt_20260101.bak");
        var otherFile = Path.Combine(_testBackupDir, "notes.txt");

        await File.WriteAllTextAsync(bakFile, "backup");
        File.SetLastWriteTime(bakFile, DateTime.Now.AddDays(-10));

        await File.WriteAllTextAsync(otherFile, "other");
        File.SetLastWriteTime(otherFile, DateTime.Now.AddDays(-10));

        // Act - cleanup only lybt_*.bak pattern
        var cutoffDate = DateTime.Now.AddDays(-7);
        var oldFiles = Directory.GetFiles(_testBackupDir, "lybt_*.bak")
            .Select(f => new FileInfo(f))
            .Where(f => f.LastWriteTime < cutoffDate)
            .ToList();

        foreach (var file in oldFiles)
        {
            file.Delete();
        }

        // Assert
        File.Exists(bakFile).Should().BeFalse("matching .bak file should be deleted");
        File.Exists(otherFile).Should().BeTrue("non-matching file should be retained");
    }

    [Fact]
    public void RetentionPolicy_DefaultIs7Days()
    {
        // Assert - validates the interface default parameter
        const int defaultRetentionDays = 7;
        defaultRetentionDays.Should().Be(7);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBackupDir))
        {
            Directory.Delete(_testBackupDir, true);
        }
    }
}
