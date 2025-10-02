using System.Text;
using AutoMapper;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;

namespace LYBT.Module.Prescriptions.Services
{
    /// <summary>
    /// 处方服务 - 简化版，包含基础CRUD、价格计算和打印格式生成
    /// 支持四种开方方式的核心功能，保持价格计算准确性
    /// </summary>
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IPrescriptionRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<PrescriptionService> _logger;

        public PrescriptionService(
            IPrescriptionRepository repository,
            IMapper mapper,
            ILogger<PrescriptionService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                // 使用优化后的查询方法，包含Items集合
                var pagedResult = await _repository.GetPagedWithDetailsAsync(page, pageSize, keyword);
                var dto = new PagedResult<PrescriptionDto>
                {
                    Items = _mapper.Map<List<PrescriptionDto>>(pagedResult.Items),
                    TotalCount = pagedResult.TotalCount,
                    CurrentPage = pagedResult.CurrentPage,
                    PageSize = pagedResult.PageSize
                };
                return ServiceResult<PagedResult<PrescriptionDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方列表失败");
                return ServiceResult<PagedResult<PrescriptionDto>>.Failure("获取处方列表失败");
            }
        }

        public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
        {
            try
            {
                // 使用优化后的查询方法，包含处方项
                var entity = await _repository.GetByIdWithItemsAsync(id);
                if (entity == null)
                    return ServiceResult<PrescriptionDto>.Failure("处方不存在");

                var dto = _mapper.Map<PrescriptionDto>(entity);
                return ServiceResult<PrescriptionDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方详情失败");
                return ServiceResult<PrescriptionDto>.Failure("获取处方详情失败");
            }
        }

        /// <summary>
        /// 创建处方 - 仅在独立创建时使用
        /// 注意：推荐通过MedicalCase聚合根创建完整的诊疗流程
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
        {
            try
            {
                var entity = _mapper.Map<PrescriptionEntity>(dto);

                // 注意：处方总价在DTO层计算，实体层不存储

                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<PrescriptionDto>(result);
                return ServiceResult<PrescriptionDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建处方失败");
                return ServiceResult<PrescriptionDto>.Failure("创建处方失败");
            }
        }

        public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionUpdateDto dto)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<PrescriptionDto>.Failure("处方不存在");

                _mapper.Map(dto, entity);
                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<PrescriptionDto>(result);
                return ServiceResult<PrescriptionDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新处方失败");
                return ServiceResult<PrescriptionDto>.Failure("更新处方失败");
            }
        }

        public async Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                // 使用优化后的查询方法，直接查询并包含Items集合
                var prescriptions = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);

                // 转换为DTO
                var prescriptionDtos = _mapper.Map<List<PrescriptionDto>>(prescriptions);

                return ServiceResult<List<PrescriptionDto>>.Success(prescriptionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取病历相关处方时发生错误，病历ID：{MedicalCaseId}", medicalCaseId);
                return ServiceResult<List<PrescriptionDto>>.Failure($"获取病历相关处方失败：{ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                var result = await _repository.DeleteAsync(id);
                return result ? ServiceResult.Success() : ServiceResult.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除处方失败");
                return ServiceResult.Failure("删除处方失败");
            }
        }

        /// <summary>
        /// 计算处方总价 - 简化的价格计算逻辑
        /// </summary>
        /// <param name="items">处方项列表</param>
        /// <param name="dosageCount">处方帖数</param>
        /// <param name="discount">折扣</param>
        /// <returns>总价</returns>
        private decimal CalculateTotalAmount(IEnumerable<LYBT.Entities.Prescriptions.PrescriptionItem> items, int dosageCount, decimal discount = 1.0m)
        {
            decimal total = 0;

            foreach (var item in items)
            {
                // 基础价格计算：单价 × 数量 × 帖数
                var itemTotal = item.UnitPrice * item.Quantity * dosageCount;
                total += itemTotal;
            }

            // 应用折扣
            return total * discount;
        }

        /// <summary>
        /// 重新计算处方价格（价格计算在DTO层处理）
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <returns>带有计算价格的处方DTO</returns>
        public async Task<ServiceResult<PrescriptionDto>> RecalculatePriceAsync(Guid prescriptionId)
        {
            try
            {
                var entity = await _repository.GetByIdWithItemsAsync(prescriptionId);
                if (entity == null)
                    return ServiceResult<PrescriptionDto>.Failure("处方不存在");

                // 注意：实体层不存储总价，价格计算在DTO层进行
                var dto = _mapper.Map<PrescriptionDto>(entity);

                // 如果DTO有TotalAmount属性，可以在这里计算
                if (entity.Items?.Any() == true)
                {
                    var calculatedTotal = CalculateTotalAmount(entity.Items, entity.DosageCount, entity.Discount);
                    // DTO映射时会自动计算总价
                }

                return ServiceResult<PrescriptionDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新计算处方价格失败");
                return ServiceResult<PrescriptionDto>.Failure("重新计算处方价格失败");
            }
        }

        /// <summary>
        /// 生成简化的处方打印格式
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <returns>打印格式字符串</returns>
        public async Task<ServiceResult<string>> GeneratePrintFormatAsync(Guid prescriptionId)
        {
            try
            {
                var entity = await _repository.GetByIdWithItemsAsync(prescriptionId);
                if (entity == null)
                    return ServiceResult<string>.Failure("处方不存在");

                var printFormat = GenerateSimplePrintFormat(entity);
                return ServiceResult<string>.Success(printFormat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成处方打印格式失败");
                return ServiceResult<string>.Failure("生成处方打印格式失败");
            }
        }

        /// <summary>
        /// 生成简单的处方打印格式
        /// </summary>
        private string GenerateSimplePrintFormat(PrescriptionEntity prescription)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"处方编号: {prescription.Id}");
            sb.AppendLine($"开方日期: {prescription.CreatedAt:yyyy-MM-dd}");
            sb.AppendLine($"适应症: {prescription.Indication}");
            sb.AppendLine();
            sb.AppendLine("药材清单:");
            sb.AppendLine("序号	药材名称	数量	单位	单价	小计");
            sb.AppendLine(new string('-', 50));

            int index = 1;
            foreach (var item in prescription.Items ?? [])
            {
                var subtotal = item.UnitPrice * item.Quantity * prescription.DosageCount;
                sb.AppendLine($"{index}	{item.HerbName}	{item.Quantity}	{item.Unit}	{item.UnitPrice:F2}	{subtotal:F2}");
                index++;
            }

            // 计算总金额
            decimal totalAmount = 0;
            foreach (var item in prescription.Items ?? [])
            {
                totalAmount += item.UnitPrice * item.Quantity * prescription.DosageCount;
            }

            // 应用折扣
            var finalAmount = totalAmount * prescription.Discount;

            sb.AppendLine(new string('-', 50));
            sb.AppendLine($"帖数: {prescription.DosageCount} 帖");
            if (prescription.Discount < 1.0m)
            {
                sb.AppendLine($"小计: {totalAmount:F2} 元");
                sb.AppendLine($"折扣: {prescription.Discount:P0}");
                sb.AppendLine($"总金额: {finalAmount:F2} 元");
            }
            else
            {
                sb.AppendLine($"总金额: {finalAmount:F2} 元");
            }

            if (!string.IsNullOrEmpty(prescription.Advice))
            {
                sb.AppendLine($"医嘱: {prescription.Advice}");
            }

            return sb.ToString();
        }
    }
}
