namespace LYBT.Desktop.Foundation.Security;

/// <summary>
/// C2: 照片加密存储服务接口
/// 使用 DPAPI 加密身份证照片，仅当前 Windows 用户可解密
/// </summary>
public interface IPhotoStorageService
{
    /// <summary>
    /// 加密并保存照片
    /// </summary>
    /// <param name="photoData">原始照片字节数据 (BMP)</param>
    /// <param name="identifier">唯一标识符 (如身份证号哈希)</param>
    /// <returns>加密文件的完整路径</returns>
    Task<string> SavePhotoAsync(byte[] photoData, string identifier);
}
