using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Services
{

    /// <summary>
    /// 智能处方服务实现 - 核心配伍和验方组合功能
    /// </summary>
    public class IntelligentPrescriptionService(ILogger<IntelligentPrescriptionService> logger) : IIntelligentPrescriptionService
    {
        private readonly ILogger<IntelligentPrescriptionService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// 智能组合多个验方模板生成处方
        /// </summary>
        public Task<ServiceResult<PrescriptionDto>> ComposeFromFormulasAsync(List<Guid> formulaIds, int dosageCount = 7)
        {
            try
            {
                if (formulaIds == null || !formulaIds.Any())
                {
                    return Task.FromResult(ServiceResult<PrescriptionDto>.Failure("请选择要组合的验方模板"));
                }

                // TODO: 实现验方组合逻辑
                // 1. 获取验方模板
                // 2. 合并药材清单
                // 3. 去重处理
                // 4. 生成新处方
                _logger.LogInformation("验方组合功能待实现 - 验方数量: {Count}", formulaIds.Count);
                return Task.FromResult(ServiceResult<PrescriptionDto>.Failure("验方组合功能开发中"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验方组合失败");
                return Task.FromResult(ServiceResult<PrescriptionDto>.Failure("验方组合失败"));
            }
        }

        /// <summary>
        /// 智能重复药材检测和处理
        /// </summary>
        public ServiceResult<List<PrescriptionItemDto>> DetectDuplicateHerbs(List<PrescriptionItemDto> items)
        {
            try
            {
                if (items == null || !items.Any())
                {
                    return ServiceResult<List<PrescriptionItemDto>>.Success(new List<PrescriptionItemDto>());
                }

                // 按药材ID分组，合并重复药材
                var mergedItems = items
                    .GroupBy(item => item.HerbId)
                    .Select(group => new PrescriptionItemDto
                    {
                        HerbId = group.Key,
                        HerbName = group.First().HerbName,
                        Quantity = group.Sum(x => x.Quantity),
                        Unit = group.First().Unit,
                        Price = group.First().Price,
                        Usage = group.First().Usage,
                        Remark = string.Join("; ", group.Select(x => x.Remark).Where(r => !string.IsNullOrEmpty(r)))
                    })
                    .ToList();

                return ServiceResult<List<PrescriptionItemDto>>.Success(mergedItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重复药材检测失败");
                return ServiceResult<List<PrescriptionItemDto>>.Failure("重复药材检测失败");
            }
        }

        /// <summary>
        /// 计算处方价格和重量
        /// </summary>
        public ServiceResult<PrescriptionCalculationDto> CalculatePrescriptionPrice(List<PrescriptionItemDto> items, int dosageCount)
        {
            try
            {
                if (items == null || !items.Any())
                {
                    return ServiceResult<PrescriptionCalculationDto>.Success(new PrescriptionCalculationDto
                    {
                        TotalPrice = 0,
                        SingleDosagePrice = 0,
                        TotalWeight = 0,
                        SingleDosageWeight = 0
                    });
                }

                var singleDosagePrice = items.Sum(item => item.Price * item.Quantity);
                var totalPrice = singleDosagePrice * dosageCount;
                var singleDosageWeight = items.Sum(item => item.Quantity);
                var totalWeight = singleDosageWeight * dosageCount;

                var result = new PrescriptionCalculationDto
                {
                    TotalPrice = totalPrice,
                    SingleDosagePrice = singleDosagePrice,
                    TotalWeight = totalWeight,
                    SingleDosageWeight = singleDosageWeight
                };

                return ServiceResult<PrescriptionCalculationDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处方价格计算失败");
                return ServiceResult<PrescriptionCalculationDto>.Failure("处方价格计算失败");
            }
        }
    }
}
