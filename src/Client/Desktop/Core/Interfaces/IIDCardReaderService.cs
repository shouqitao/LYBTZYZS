using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Threading.Tasks;

namespace LYBT.Desktop.Core.Interfaces
{
    /// <summary>
    /// 身份证读卡器服务接口
    /// 预留接口，用于后续集成身份证读卡器硬件
    /// </summary>
    public interface IIDCardReaderService
    {
        /// <summary>
        /// 检查读卡器是否已连接
        /// </summary>
        Task<bool> IsConnectedAsync();

        /// <summary>
        /// 连接读卡器
        /// </summary>
        Task<bool> ConnectAsync();

        /// <summary>
        /// 断开读卡器连接
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// 读取身份证信息
        /// </summary>
        Task<IDCardInfo?> ReadCardAsync();

        /// <summary>
        /// 读卡器状态变化事件
        /// </summary>
        event EventHandler<IDCardReaderStatusChangedEventArgs>? StatusChanged;

        /// <summary>
        /// 读卡成功事件
        /// </summary>
        event EventHandler<IDCardReadEventArgs>? CardRead;
    }

    /// <summary>
    /// 身份证信息
    /// </summary>
    public class IDCardInfo
    {
        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 性别
        /// </summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>
        /// 民族
        /// </summary>
        public string Nation { get; set; } = string.Empty;

        /// <summary>
        /// 出生日期
        /// </summary>
        public DateTime BirthDate { get; set; }

        /// <summary>
        /// 身份证号码
        /// </summary>
        public string IDNumber { get; set; } = string.Empty;

        /// <summary>
        /// 住址
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 签发机关
        /// </summary>
        public string IssuingAuthority { get; set; } = string.Empty;

        /// <summary>
        /// 有效期开始日期
        /// </summary>
        public DateTime ValidFrom { get; set; }

        /// <summary>
        /// 有效期结束日期
        /// </summary>
        public DateTime ValidTo { get; set; }

        /// <summary>
        /// 照片（Base64编码）
        /// </summary>
        public string? PhotoBase64 { get; set; }

        /// <summary>
        /// 计算年龄
        /// </summary>
        public int Age => CalculateAge();

        private int CalculateAge()
        {
            var today = DateTime.Today;
            var age = today.Year - BirthDate.Year;
            if (BirthDate.Date > today.AddYears(-age))
                age--;
            return age;
        }

        /// <summary>
        /// 验证身份证号码格式
        /// </summary>
        public bool IsValidIDNumber()
        {
            if (string.IsNullOrWhiteSpace(IDNumber))
                return false;

            // 简单的18位身份证号验证
            if (IDNumber.Length != 18)
                return false;

            // 可以添加更详细的验证逻辑
            return true;
        }
    }

    /// <summary>
    /// 读卡器状态变化事件参数
    /// </summary>
    public class IDCardReaderStatusChangedEventArgs : EventArgs
    {
        public IDCardReaderStatus OldStatus { get; set; }
        public IDCardReaderStatus NewStatus { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// 读卡器状态
    /// </summary>
    public enum IDCardReaderStatus
    {
        /// <summary>
        /// 未连接
        /// </summary>
        Disconnected,

        /// <summary>
        /// 正在连接
        /// </summary>
        Connecting,

        /// <summary>
        /// 已连接
        /// </summary>
        Connected,

        /// <summary>
        /// 正在读卡
        /// </summary>
        Reading,

        /// <summary>
        /// 错误
        /// </summary>
        Error
    }

    /// <summary>
    /// 读卡事件参数
    /// </summary>
    public class IDCardReadEventArgs : EventArgs
    {
        public IDCardInfo? CardInfo { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}