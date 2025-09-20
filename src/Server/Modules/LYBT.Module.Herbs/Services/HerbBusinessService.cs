using AutoMapper;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Services
{
    /// <summary>
    /// 药材业务逻辑服务 - UltraThink架构
    /// 职责：导入导出、批量操作、业务规则处理
    /// </summary>
    public class HerbBusinessService : IHerbBusinessService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<HerbBusinessService> _logger;

        public HerbBusinessService(
            AppDbContext context,
            IMapper mapper,
            ILogger<HerbBusinessService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 批量导入药材数据 - Phase C1 事务优化：50条/批短事务模式
        /// </summary>
        public async Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs)
        {
            try
            {
                if (herbs == null || herbs.Count == 0)
                {
                    return ServiceResult<int>.Success(0);
                }

                const int BATCH_SIZE = 50; // Phase C1: 小诊所优化，50条/批减少事务时间
                var totalImportCount = 0;
                var totalErrors = new List<string>();
                var batches = SplitIntoBatches(herbs, BATCH_SIZE);

                _logger.LogInformation("开始分批导入药材 - 总数: {Total}, 批次数: {BatchCount}, 每批: {BatchSize}条",
                    herbs.Count, batches.Count, BATCH_SIZE);

                // 分批处理，每批使用独立的短事务
                for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
                {
                    var batch = batches[batchIndex];
                    var batchResult = await ImportHerbsBatch(batch, batchIndex + 1, BATCH_SIZE);

                    totalImportCount += batchResult.ImportCount;
                    totalErrors.AddRange(batchResult.Errors);
                }

                _logger.LogInformation(
                    "药材批量导入完成 - 成功: {SuccessCount}, 失败: {ErrorCount}, 批次: {BatchCount}",
                    totalImportCount, totalErrors.Count, batches.Count);

                if (totalErrors.Count > 0 && totalImportCount == 0)
                {
                    return ServiceResult<int>.Failure($"导入失败，所有记录都有错误：{string.Join("; ", totalErrors.Take(5))}");
                }

                if (totalErrors.Count > 0)
                {
                    var errorSummary = totalErrors.Count > 5
                        ? $"{string.Join("; ", totalErrors.Take(5))}... (共{totalErrors.Count}个错误)"
                        : string.Join("; ", totalErrors);
                    return ServiceResult<int>.Failure($"部分导入成功 {totalImportCount} 条，失败 {totalErrors.Count} 条。错误详情：{errorSummary}");
                }

                return ServiceResult<int>.Success(totalImportCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入药材异常");
                return ServiceResult<int>.Failure($"批量导入药材异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 导入单个批次的药材 - Phase C1 短事务实现.
        /// </summary>
        private async Task<(int ImportCount, List<string> Errors)> ImportHerbsBatch(
            List<HerbImportDto> batch, int batchNumber, int batchSize)
        {
            return await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var importCount = 0;
                    var errors = new List<string>();
                    var baseIndex = (batchNumber - 1) * batchSize;

                    foreach (var (importDto, index) in batch.Select((dto, i) => (dto, i)))
                    {
                        try
                        {
                            var rowNumber = baseIndex + index + 1;

                            // 验证导入数据
                            var validationResult = ValidateImportDto(importDto);
                            if (!validationResult.IsSuccess)
                            {
                                errors.Add($"行 {rowNumber}: {validationResult.ErrorMessage}");
                                continue;
                            }

                            // 检查重复名称
                            var existingHerb = await _context.Herbs
                                .FirstOrDefaultAsync(h => h.Name == importDto.Name && h.Status != CommonStatus.Disabled);

                            if (existingHerb != null)
                            {
                                errors.Add($"行 {rowNumber}: 药材名称 '{importDto.Name}' 已存在");
                                continue;
                            }

                            // 创建药材实体
                            var herb = new Herb
                            {
                                Id = Guid.NewGuid(),
                                Name = importDto.Name,
                                PinYinCode = GenerateSimplePinyinCode(importDto.Name),
                                Origin = importDto.Origin ?? string.Empty,
                                Spec = importDto.Spec ?? string.Empty,
                                Unit = importDto.Unit ?? "g",
                                Price = importDto.Price,
                                Effect = importDto.Effect ?? string.Empty,
                                Usage = string.Empty, // 导入时默认为空
                                Remark = importDto.Remark ?? string.Empty,
                                Status = CommonStatus.Enabled,
                            };

                            _context.Herbs.Add(herb);
                            importCount++;
                        }
                        catch (Exception ex)
                        {
                            var rowNumber = baseIndex + index + 1;
                            errors.Add($"行 {rowNumber}: 处理失败 - {ex.Message}");
                            _logger.LogError(ex, "导入药材失败: {HerbName}, 批次: {BatchNumber}", importDto.Name, batchNumber);
                        }
                    }

                    if (importCount > 0)
                    {
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _logger.LogInformation("批次 {BatchNumber} 导入药材成功: {ImportCount}条", batchNumber, importCount);
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        _logger.LogWarning("批次 {BatchNumber} 没有成功导入任何药材", batchNumber);
                    }

                    return (ImportCount: importCount, Errors: errors);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning(ex, "批次 {BatchNumber} 导入药材并发冲突", batchNumber);
                    return (ImportCount: 0, Errors: new List<string> { $"批次 {batchNumber}: 数据已被其他用户修改，请重试" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "批次 {BatchNumber} 导入药材异常", batchNumber);
                    return (ImportCount: 0, Errors: new List<string> { $"批次 {batchNumber}: 导入异常 - {ex.Message}" });
                }
            });
        }

        /// <summary>
        /// 将列表拆分为指定大小的批次 - Phase C1 辅助方法
        /// </summary>
        private static List<List<T>> SplitIntoBatches<T>(List<T> items, int batchSize)
        {
            var batches = new List<List<T>>();
            for (int i = 0; i < items.Count; i += batchSize)
            {
                var batch = items.Skip(i).Take(batchSize).ToList();
                batches.Add(batch);
            }
            return batches;
        }

        /// <summary>
        /// 批量更新状态 - 简化版本，直接使用参数
        /// </summary>
        public async Task<ServiceResult<bool>> BatchUpdateStatusAsync(List<Guid> ids, bool status, string? reason = null)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                {
                    return ServiceResult<bool>.Success(true); // 空操作视为成功
                }

                var targetStatus = status ? CommonStatus.Enabled : CommonStatus.Disabled;

                // 使用EF Core的ExecuteUpdateAsync进行批量更新
                var affectedRows = await _context.Herbs
                    .Where(h => ids.Contains(h.Id))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(h => h.Status, targetStatus));

                _logger.LogInformation(
                    "批量更新药材状态成功: 更新{Count}条记录为{Status}",
                    affectedRows, status ? "启用" : "禁用");

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新药材状态失败");
                return ServiceResult<bool>.Failure($"批量更新药材状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 软删除药材
        /// </summary>
        public async Task<ServiceResult<bool>> SoftDeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("药材ID不能为空");
                }

                var herb = await _context.Herbs.FindAsync(id);
                if (herb == null)
                {
                    return ServiceResult<bool>.Failure("药材不存在");
                }

                // 检查是否被处方引用
                var isReferencedInPrescriptions = await _context.PrescriptionItems
                    .AnyAsync(pi => pi.HerbId == id);

                if (isReferencedInPrescriptions)
                {
                    return ServiceResult<bool>.Failure("该药材已被处方引用，不能删除。建议设置为禁用状态。");
                }

                // 软删除
                herb.Status = CommonStatus.Disabled;
                _context.Herbs.Update(herb);
                await _context.SaveChangesAsync();

                _logger.LogInformation("软删除药材成功: {HerbName} ({HerbId})", herb.Name, herb.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "软删除药材失败: {HerbId}", id);
                return ServiceResult<bool>.Failure($"软删除药材失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建药材（带自动拼音码生成）
        /// </summary>
        public async Task<ServiceResult<HerbDto>> CreateHerbWithAutoCodeAsync(HerbCreateDto dto)
        {
            try
            {
                // 数据验证
                var validationResult = ValidateCreateDto(dto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<HerbDto>.Failure(validationResult.ErrorMessage ?? "数据验证失败");
                }

                // 检查名称重复
                var existingHerb = await _context.Herbs
                    .FirstOrDefaultAsync(h => h.Name == dto.Name && h.Status != CommonStatus.Disabled);

                if (existingHerb != null)
                {
                    return ServiceResult<HerbDto>.Failure($"药材名称 '{dto.Name}' 已存在");
                }

                // 自动生成拼音码
                var pinyinCode = string.IsNullOrWhiteSpace(dto.PinYinCode)
                    ? GenerateSimplePinyinCode(dto.Name)
                    : dto.PinYinCode;

                // 创建新药材
                var herb = new Herb
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    PinYinCode = pinyinCode,
                    Origin = dto.Origin ?? string.Empty,
                    Spec = dto.Spec ?? string.Empty,
                    Unit = dto.Unit ?? "g",
                    Price = dto.Price,
                    Effect = dto.Effect ?? string.Empty,
                    Usage = dto.Usage ?? string.Empty,
                    Remark = dto.Remark ?? string.Empty,
                    Status = CommonStatus.Enabled
                };

                _context.Herbs.Add(herb);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "创建药材成功: {HerbName} ({HerbId}), 拼音码: {PinyinCode}",
                    herb.Name, herb.Id, herb.PinYinCode);

                var resultDto = _mapper.Map<HerbDto>(herb);
                return ServiceResult<HerbDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建药材失败: {HerbName}", dto.Name);
                return ServiceResult<HerbDto>.Failure($"创建药材失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新药材信息
        /// </summary>
        public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<HerbDto>.Failure("药材ID不能为空");
                }

                if (dto == null)
                {
                    return ServiceResult<HerbDto>.Failure("更新信息不能为空");
                }

                var herb = await _context.Herbs
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (herb == null)
                {
                    return ServiceResult<HerbDto>.Failure("药材不存在");
                }

                if (herb.Status == CommonStatus.Disabled)
                {
                    return ServiceResult<HerbDto>.Failure("已删除的药材不能修改");
                }

                // 检查名称重复（排除自己）
                if (!string.IsNullOrEmpty(dto.Name) && dto.Name != herb.Name)
                {
                    var existingHerb = await _context.Herbs
                        .FirstOrDefaultAsync(h => h.Name == dto.Name && h.Id != id && h.Status != CommonStatus.Disabled);

                    if (existingHerb != null)
                    {
                        return ServiceResult<HerbDto>.Failure($"药材名称 '{dto.Name}' 已存在");
                    }
                }

                // 更新药材信息
                if (!string.IsNullOrEmpty(dto.Name))
                {
                    herb.Name = dto.Name;
                    herb.PinYinCode = GenerateSimplePinyinCode(dto.Name);
                }
                if (!string.IsNullOrEmpty(dto.Origin)) herb.Origin = dto.Origin;
                if (!string.IsNullOrEmpty(dto.Spec)) herb.Spec = dto.Spec;
                if (!string.IsNullOrEmpty(dto.Unit)) herb.Unit = dto.Unit;
                if (dto.Price > 0) herb.Price = dto.Price;
                if (!string.IsNullOrEmpty(dto.Effect)) herb.Effect = dto.Effect;
                if (!string.IsNullOrEmpty(dto.Usage)) herb.Usage = dto.Usage;
                if (!string.IsNullOrEmpty(dto.Remark)) herb.Remark = dto.Remark;

                _context.Herbs.Update(herb);
                await _context.SaveChangesAsync();

                _logger.LogInformation("更新药材成功: {HerbName} ({HerbId})", herb.Name, herb.Id);

                var resultDto = _mapper.Map<HerbDto>(herb);
                return ServiceResult<HerbDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新药材失败: {Id}", id);
                return ServiceResult<HerbDto>.Failure($"更新药材失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置药材启用/禁用状态
        /// </summary>
        public async Task<ServiceResult<bool>> SetStatusAsync(Guid id, bool isActive)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("药材ID不能为空");
                }

                var herb = await _context.Herbs.FindAsync(id);
                if (herb == null)
                {
                    return ServiceResult<bool>.Failure("药材不存在");
                }

                var newStatus = isActive ? CommonStatus.Enabled : CommonStatus.Disabled;

                if (herb.Status == newStatus)
                {
                    var currentStatusText = isActive ? "启用" : "禁用";
                    return ServiceResult<bool>.Success(true);
                }

                herb.Status = newStatus;
                _context.Herbs.Update(herb);
                await _context.SaveChangesAsync();

                var statusText = isActive ? "启用" : "禁用";
                _logger.LogInformation("{Status}药材成功: {HerbName} ({HerbId})", statusText, herb.Name, herb.Id);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置药材状态失败: {HerbId}, {IsActive}", id, isActive);
                return ServiceResult<bool>.Failure($"设置药材状态失败: {ex.Message}");
            }
        }

        #region 私有方法

        /// <summary>
        /// 验证导入DTO
        /// </summary>
        private ServiceResult<bool> ValidateImportDto(HerbImportDto dto)
        {
            if (dto == null)
            {
                return ServiceResult<bool>.Failure("药材信息不能为空");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return ServiceResult<bool>.Failure("药材名称不能为空");
            }

            if (dto.Name.Length > 50)
            {
                return ServiceResult<bool>.Failure("药材名称不能超过50个字符");
            }

            if (dto.Price <= 0)
            {
                return ServiceResult<bool>.Failure("药材价格必须大于0");
            }

            if (dto.Price > 9999.99m)
            {
                return ServiceResult<bool>.Failure("药材价格不能超过9999.99");
            }

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证创建DTO
        /// </summary>
        private ServiceResult<bool> ValidateCreateDto(HerbCreateDto dto)
        {
            if (dto == null)
            {
                return ServiceResult<bool>.Failure("药材信息不能为空");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return ServiceResult<bool>.Failure("药材名称不能为空");
            }

            if (dto.Name.Length > 50)
            {
                return ServiceResult<bool>.Failure("药材名称不能超过50个字符");
            }

            if (dto.Price <= 0)
            {
                return ServiceResult<bool>.Failure("药材价格必须大于0");
            }

            if (dto.Price > 9999.99m)
            {
                return ServiceResult<bool>.Failure("药材价格不能超过9999.99");
            }

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 解析状态字符串
        /// </summary>
        private CommonStatus ParseStatus(string statusStr)
        {
            if (string.IsNullOrWhiteSpace(statusStr))
            {
                return CommonStatus.Enabled;
            }

            return statusStr.ToLower() switch
            {
                "enabled" => CommonStatus.Enabled,
                "disabled" => CommonStatus.Disabled,
                "启用" => CommonStatus.Enabled,
                "禁用" => CommonStatus.Disabled,
                _ => CommonStatus.Enabled
            };
        }

        /// <summary>
        /// 生成简单拼音码 - 基础实现
        /// </summary>
        private string GenerateSimplePinyinCode(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            // 简单拼音码生成：取每个字的首字母
            // 实际项目中可能需要更复杂的拼音转换库
            var result = string.Empty;
            foreach (char c in name)
            {
                if (char.IsLetter(c))
                {
                    result += char.ToUpper(c);
                }
                else if (c is >= (char)0x4e00 and <= (char)0x9fff) // 中文字符范围
                {
                    // 简化处理：中文字符用首字母代替
                    // 实际应该用拼音库转换，这里使用常见中文药材的首字母
                    result += GetChineseCharacterInitial(c);
                }
            }

            return result.Length > 10 ? result.Substring(0, 10) : result;
        }

        /// <summary>
        /// 获取中文字符首字母 - 简化版
        /// </summary>
        private char GetChineseCharacterInitial(char c)
        {
            // 简化实现：根据unicode编码范围映射到字母
            // 实际项目中应该使用专业的拼音转换库
            var code = (int)c;

            return code switch
            {
                >= 0x4e00 and < 0x4f00 => 'A',
                >= 0x4f00 and < 0x5000 => 'B',
                >= 0x5000 and < 0x5100 => 'C',
                >= 0x5100 and < 0x5200 => 'D',
                >= 0x5200 and < 0x5300 => 'E',
                >= 0x5300 and < 0x5400 => 'F',
                >= 0x5400 and < 0x5500 => 'G',
                >= 0x5500 and < 0x5600 => 'H',
                >= 0x5600 and < 0x5700 => 'J',
                >= 0x5700 and < 0x5800 => 'K',
                >= 0x5800 and < 0x5900 => 'L',
                >= 0x5900 and < 0x5a00 => 'M',
                >= 0x5a00 and < 0x5b00 => 'N',
                >= 0x5b00 and < 0x5c00 => 'P',
                >= 0x5c00 and < 0x5d00 => 'Q',
                >= 0x5d00 and < 0x5e00 => 'R',
                >= 0x5e00 and < 0x5f00 => 'S',
                >= 0x5f00 and < 0x6000 => 'T',
                >= 0x6000 and < 0x6100 => 'W',
                >= 0x6100 and < 0x6200 => 'X',
                >= 0x6200 and < 0x6300 => 'Y',
                _ => 'Z'
            };
        }

        #endregion 私有方法
    }
}
