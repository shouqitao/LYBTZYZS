using AutoMapper;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Common;
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
    public class HerbBusinessService
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
        /// 批量导入药材数据
        /// </summary>
        public async Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs)
        {
            try
            {
                if (herbs == null || herbs.Count == 0)
                {
                    return ServiceResult<int>.Success(0);
                }

                var importCount = 0;
                var errors = new List<string>();

                using var transaction = await _context.Database.BeginTransactionAsync();

                foreach (var importDto in herbs)
                {
                    try
                    {
                        // 验证导入数据
                        var validationResult = ValidateImportDto(importDto);
                        if (!validationResult.IsSuccess)
                        {
                            errors.Add($"行 {importCount + 1}: {validationResult.ErrorMessage}");
                            continue;
                        }

                        // 检查重复名称
                        var existingHerb = await _context.Herbs
                            .FirstOrDefaultAsync(h => h.Name == importDto.Name && h.Status != CommonStatus.Disabled);

                        if (existingHerb != null)
                        {
                            errors.Add($"行 {importCount + 1}: 药材名称 '{importDto.Name}' 已存在");
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
                            Status = CommonStatus.Enabled
                        };

                        _context.Herbs.Add(herb);
                        importCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"行 {importCount + 1}: 处理失败 - {ex.Message}");
                        _logger.LogError(ex, "导入药材失败: {HerbName}", importDto.Name);
                    }
                }

                if (importCount > 0)
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _logger.LogInformation("批量导入药材成功: {ImportCount}条", importCount);
                }
                else
                {
                    await transaction.RollbackAsync();
                }

                if (errors.Count > 0)
                {
                    var errorMessage = $"导入完成，成功 {importCount} 条，失败 {errors.Count} 条。错误详情：{string.Join("; ", errors)}";
                    return ServiceResult<int>.Failure(errorMessage);
                }

                return ServiceResult<int>.Success(importCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入药材异常");
                return ServiceResult<int>.Failure($"批量导入药材异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量更新状态
        /// </summary>
        public async Task<ServiceResult<bool>> BatchUpdateStatusAsync(BatchStatusUpdateDto dto)
        {
            try
            {
                if (dto?.Ids == null || dto.Ids.Count == 0)
                {
                    return ServiceResult<bool>.Success(true); // 空操作视为成功
                }

                var status = dto.Status ? CommonStatus.Enabled : CommonStatus.Disabled;

                // 使用EF Core的ExecuteUpdateAsync进行批量更新
                var affectedRows = await _context.Herbs
                    .Where(h => dto.Ids.Contains(h.Id))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(h => h.Status, status));

                _logger.LogInformation(
                    "批量更新药材状态成功: 更新{Count}条记录为{Status}",
                    affectedRows, dto.Status ? "启用" : "禁用");

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
