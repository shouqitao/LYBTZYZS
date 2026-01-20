using System.Runtime.InteropServices;
using System.Text;

namespace LYBT.Desktop.CardReader.Native;

/// <summary>
/// 华大HD100身份证读卡器原生方法
/// P/Invoke包装器，封装HDstdapi.dll
/// </summary>
internal static class HuaDaNativeMethods
{
    private const string DllName = "HDstdapi.dll";

    #region 设备控制

    /// <summary>
    /// 初始化设备通讯
    /// </summary>
    /// <param name="port">端口号: 1-16=串口, 1001=USB</param>
    /// <returns>1=成功, 其他=失败</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern int HD_InitComm(int port);

    /// <summary>
    /// 关闭设备
    /// </summary>
    /// <returns>1=成功</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern int HD_CloseComm();

    /// <summary>
    /// 卡片认证
    /// </summary>
    /// <param name="type">认证类型（通常为1）</param>
    /// <returns>0=成功, 其他=失败</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern int HD_Authenticate(int type);

    #endregion

    #region 读卡操作

    /// <summary>
    /// 读取卡片（需先调用HD_Authenticate）
    /// </summary>
    /// <returns>0=成功</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern long HD_ReadCard();

    /// <summary>
    /// 一次性读取所有基本信息
    /// </summary>
    /// <param name="bmpData">照片保存路径（如 D:\photo.bmp）</param>
    /// <param name="name">姓名</param>
    /// <param name="sex">性别</param>
    /// <param name="nation">民族</param>
    /// <param name="birth">出生日期（YYYYMMDD）</param>
    /// <param name="address">住址</param>
    /// <param name="certNo">身份证号</param>
    /// <param name="department">签发机关</param>
    /// <param name="effectData">有效期起</param>
    /// <param name="expire">有效期止</param>
    /// <returns>0=成功, 其他=失败</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern int HD_Read_BaseMsg(
        StringBuilder bmpData,
        StringBuilder name,
        StringBuilder sex,
        StringBuilder nation,
        StringBuilder birth,
        StringBuilder address,
        StringBuilder certNo,
        StringBuilder department,
        StringBuilder effectData,
        StringBuilder expire);

    #endregion

    #region 单独获取字段（需先调用HD_ReadCard）

    /// <summary>获取姓名</summary>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern IntPtr GetName();

    /// <summary>获取身份证号</summary>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern IntPtr GetCertNo();

    /// <summary>获取性别</summary>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern IntPtr GetSex();

    /// <summary>获取民族</summary>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern IntPtr GetNation();

    /// <summary>获取出生日期</summary>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern IntPtr GetBirth();

    /// <summary>获取住址</summary>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern IntPtr GetAddress();

    /// <summary>获取签发机关</summary>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern IntPtr GetDepartemt();

    /// <summary>获取有效期起</summary>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern IntPtr GetEffectDate();

    /// <summary>获取有效期止</summary>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern IntPtr GetExpireDate();

    /// <summary>获取证件类型（0=居民身份证，1=外国人永久居留证，2=港澳台居民居住证）</summary>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    public static extern int GetCardType();

    #endregion

    #region 照片操作

    /// <summary>
    /// 获取BMP格式照片数据
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern IntPtr GetBmpFileData();

    /// <summary>
    /// 保存照片到指定路径
    /// </summary>
    /// <param name="bmpFilePath">保存路径（如 D:\photo.bmp）</param>
    /// <returns>0=成功</returns>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    public static extern int GetBmpFile(string bmpFilePath);

    #endregion

    #region 辅助方法

    /// <summary>
    /// 将IntPtr转换为字符串
    /// </summary>
    public static string PtrToString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
            return string.Empty;

        return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
    }

    /// <summary>
    /// 检查DLL是否存在
    /// </summary>
    public static bool IsDllAvailable()
    {
        try
        {
            // 尝试加载DLL
            var handle = NativeLibrary.Load(DllName);
            NativeLibrary.Free(handle);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
