using LYBT.Module.Prescriptions.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Services
{
    /// <summary>
    /// 处方编号生成服务
    /// Issue #1551: 处方自动编号功能
    /// </summary>
    public class PrescriptionNumberService : IPrescriptionNumberService
    {
        private readonly IPrescriptionRepository _prescriptionRepository;
        private readonly ILogger<PrescriptionNumberService> _logger;

        public PrescriptionNumberService(
            IPrescriptionRepository prescriptionRepository,
            ILogger<PrescriptionNumberService> logger)
        {
            _prescriptionRepository = prescriptionRepository;
            _logger = logger;
        }

        /// <summary>
        /// 生成处方编号
        /// 格式：RX-YYYYMMDD-NNNN
        /// 例如：RX-20251021-0001
        /// </summary>
        /// <param name="date">指定日期（通常为当前日期）</param>
        /// <returns>生成的处方编号</returns>
        public async Task<string> GenerateNumberAsync(DateTime date)
        {
            // 格式化日期前缀（RX-YYYYMMDD）
            var datePrefix = $"RX-{date:yyyyMMdd}";

            // 获取当日已存在的最大序号
            var maxSequence = await GetMaxSequenceForDateAsync(date);

            // 生成新序号（最大序号+1，从0001开始）
            var newSequence = maxSequence + 1;

            // 组合完整编号
            var prescriptionNumber = $"{datePrefix}-{newSequence:D4}";

            _logger.LogInformation("生成处方编号: {PrescriptionNumber} (日期: {Date}, 序号: {Sequence})",
                prescriptionNumber, date.ToString("yyyy-MM-dd"), newSequence);

            return prescriptionNumber;
        }

        /// <summary>
        /// 验证处方编号格式是否有效
        /// </summary>
        /// <param name="prescriptionNumber">待验证的处方编号</param>
        /// <returns>是否有效</returns>
        public bool ValidateNumberFormat(string prescriptionNumber)
        {
            if (string.IsNullOrWhiteSpace(prescriptionNumber))
                return false;

            // 格式：RX-YYYYMMDD-NNNN（总长16字符：RX=2 + -=1 + YYYYMMDD=8 + -=1 + NNNN=4）
            // 示例：RX-20251021-0001
            if (prescriptionNumber.Length < 16)
                return false;

            // 验证前缀
            if (!prescriptionNumber.StartsWith("RX-"))
                return false;

            // 验证中间分隔符（位置11）
            if (prescriptionNumber.Length > 11 && prescriptionNumber[11] != '-')
                return false;

            // 验证日期部分（8位数字）
            if (prescriptionNumber.Length < 11)
                return false;
            var datePart = prescriptionNumber.Substring(3, 8);
            if (!datePart.All(char.IsDigit))
                return false;

            // 验证日期有效性
            if (!DateTime.TryParseExact(datePart, "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out _))
                return false;

            // 验证序号部分（正好4位数字）
            if (prescriptionNumber.Length != 16)
                return false;
            var sequencePart = prescriptionNumber.Substring(12, 4);
            if (!sequencePart.All(char.IsDigit))
                return false;

            return true;
        }

        /// <summary>
        /// 获取指定日期的最大序号
        /// </summary>
        /// <param name="date">指定日期</param>
        /// <returns>当日最大序号（如无记录则返回0）</returns>
        private async Task<int> GetMaxSequenceForDateAsync(DateTime date)
        {
            // 构造日期前缀用于查询
            var datePrefix = $"RX-{date:yyyyMMdd}-";

            // 查询当日所有处方编号
            var todayNumbers = await _prescriptionRepository.GetPrescriptionNumbersByPrefixAsync(datePrefix);

            if (!todayNumbers.Any())
                return 0; // 当日首个处方，序号从1开始

            // 提取所有序号并找到最大值
            var sequences = todayNumbers
                .Select(num =>
                {
                    // 提取最后4位序号
                    var sequencePart = num.Substring(12, 4);
                    return int.TryParse(sequencePart, out var seq) ? seq : 0;
                })
                .ToList();

            return sequences.Max();
        }
    }
}
