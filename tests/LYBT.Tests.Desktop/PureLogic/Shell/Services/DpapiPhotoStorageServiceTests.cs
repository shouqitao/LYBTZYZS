using System.IO;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.PureLogic.Shell.Services;

/// <summary>
/// C2: DpapiPhotoStorageService 单元测试
/// 验证 DPAPI 加密/解密照片数据的正确性
/// </summary>
public class DpapiPhotoStorageServiceTests : IDisposable
{
    private readonly DpapiPhotoStorageService _sut;
    private readonly List<string> _createdFiles = new();

    public DpapiPhotoStorageServiceTests()
    {
        var logger = Substitute.For<ILogger<DpapiPhotoStorageService>>();
        _sut = new DpapiPhotoStorageService(logger);
    }

    public void Dispose()
    {
        foreach (var file in _createdFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    [Fact]
    public async Task SavePhotoAsync_WithValidData_ReturnsEncryptedFilePath()
    {
        // Arrange
        var photoData = new byte[] { 0x42, 0x4D, 0x01, 0x02, 0x03 };
        var identifier = $"test_{Guid.NewGuid():N}";

        // Act
        var filePath = await _sut.SavePhotoAsync(photoData, identifier);
        _createdFiles.Add(filePath);

        // Assert
        Assert.NotEmpty(filePath);
        Assert.True(File.Exists(filePath));
        Assert.EndsWith(".enc", filePath);

        // 加密后的数据应与原始数据不同
        var encryptedBytes = await File.ReadAllBytesAsync(filePath);
        Assert.NotEqual(photoData, encryptedBytes);
    }

    [Fact]
    public async Task LoadPhotoAsync_AfterSave_ReturnsOriginalData()
    {
        // Arrange
        var originalData = new byte[256];
        Random.Shared.NextBytes(originalData);
        var identifier = $"roundtrip_{Guid.NewGuid():N}";

        var filePath = await _sut.SavePhotoAsync(originalData, identifier);
        _createdFiles.Add(filePath);

        // Act
        var loadedData = await _sut.LoadPhotoAsync(filePath);

        // Assert
        Assert.NotNull(loadedData);
        Assert.Equal(originalData, loadedData);
    }

    [Fact]
    public async Task LoadPhotoAsync_WithNonExistentFile_ReturnsNull()
    {
        // Act
        var result = await _sut.LoadPhotoAsync(@"C:\nonexistent\photo.enc");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadPhotoAsync_WithEmptyPath_ReturnsNull()
    {
        // Act
        var result = await _sut.LoadPhotoAsync("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeletePhotoAsync_ExistingFile_DeletesAndReturnsTrue()
    {
        // Arrange
        var photoData = new byte[] { 0x01, 0x02, 0x03 };
        var identifier = $"delete_{Guid.NewGuid():N}";
        var filePath = await _sut.SavePhotoAsync(photoData, identifier);

        // Act
        var result = await _sut.DeletePhotoAsync(filePath);

        // Assert
        Assert.True(result);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeletePhotoAsync_NonExistentFile_ReturnsTrue()
    {
        // Act
        var result = await _sut.DeletePhotoAsync(@"C:\nonexistent\photo.enc");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task PhotoExists_AfterSave_ReturnsTrue()
    {
        // Arrange
        var photoData = new byte[] { 0x01, 0x02 };
        var identifier = $"exists_{Guid.NewGuid():N}";
        var filePath = await _sut.SavePhotoAsync(photoData, identifier);
        _createdFiles.Add(filePath);

        // Act & Assert
        Assert.True(_sut.PhotoExists(filePath));
    }

    [Fact]
    public void PhotoExists_WithEmptyPath_ReturnsFalse()
    {
        Assert.False(_sut.PhotoExists(""));
        Assert.False(_sut.PhotoExists(null!));
    }

    [Fact]
    public async Task SavePhotoAsync_WithEmptyData_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.SavePhotoAsync(Array.Empty<byte>(), "test"));
    }

    [Fact]
    public async Task SavePhotoAsync_WithNullData_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.SavePhotoAsync(null!, "test"));
    }

    [Fact]
    public async Task SavePhotoAsync_SameIdentifier_OverwritesFile()
    {
        // Arrange
        var data1 = new byte[] { 0x01, 0x02 };
        var data2 = new byte[] { 0x03, 0x04, 0x05 };
        var identifier = $"overwrite_{Guid.NewGuid():N}";

        // Act
        var path1 = await _sut.SavePhotoAsync(data1, identifier);
        _createdFiles.Add(path1);
        var path2 = await _sut.SavePhotoAsync(data2, identifier);

        // Assert - 同一标识符产生同一路径
        Assert.Equal(path1, path2);

        // 读回的应该是最新数据
        var loaded = await _sut.LoadPhotoAsync(path2);
        Assert.Equal(data2, loaded);
    }
}
