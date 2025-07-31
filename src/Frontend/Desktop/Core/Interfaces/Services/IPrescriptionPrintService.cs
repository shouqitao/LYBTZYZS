using System.Threading.Tasks;
using LYBT.Shared.Models.Common;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 处方打印服务接口
    /// </summary>
    public interface IPrescriptionPrintService
    {
        /// <summary>
        /// 预览处方
        /// </summary>
        Task<PreviewResult> PreviewPrescriptionAsync(object medicalRecord);

        /// <summary>
        /// 打印处方
        /// </summary>
        Task<bool> PrintPrescriptionAsync(object medicalRecord);

        /// <summary>
        /// 保存为PDF
        /// </summary>
        Task<bool> SaveAsPdfAsync(object medicalRecord, string fileName);
    }

    /// <summary>
    /// 预览结果
    /// </summary>
    public class PreviewResult
    {
        public string Content { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}