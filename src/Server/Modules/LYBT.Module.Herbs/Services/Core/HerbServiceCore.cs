using System;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Herbs.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Herbs.Services.Core
{
    /// <summary>
    /// 药材核心CRUD服务 - UltraThink架构
    /// 职责：基础增删改查操作，数据验证，状态管理
    /// </summary>
    public class HerbServiceCore
    {
        private readonly AppDbContext _context;
        private readonly IHerbRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<HerbServiceCore> _logger;

        public HerbServiceCore(
            AppDbContext context,
            IHerbRepository repository,
            IMapper mapper,
            ILogger<HerbServiceCore> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<HerbDto>.Failure("药材ID不能为空");

                var herb = await _repository.GetByIdAsync(id);
                if (herb == null)
                    return ServiceResult<HerbDto>.Failure("药材不存在");

                var dto = _mapper.Map<HerbDto>(herb);
                return ServiceResult<HerbDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材详情失败: {HerbId}", id);
                return ServiceResult<HerbDto>.Failure($"获取药材详情失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建药材 - 自动生成拼音码
        /// </summary>
        public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto)
        {
            try
            {
                // 数据验证
                var validationResult = ValidateCreateDto(dto);
                if (!validationResult.IsSuccess)
                    return ServiceResult<HerbDto>.Failure(validationResult.ErrorMessage);

                // 检查名称重复
                var existingHerb = await _context.Herbs
                    .FirstOrDefaultAsync(h => h.Name == dto.Name && h.Status != CommonStatus.Disabled);
                
                if (existingHerb != null)
                    return ServiceResult<HerbDto>.Failure($"药材名称 '{dto.Name}' 已存在");

                // 创建新药材
                var herb = new Herb
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    PinYinCode = string.IsNullOrWhiteSpace(dto.PinYinCode) 
                        ? GenerateSimplePinyinCode(dto.Name) 
                        : dto.PinYinCode,
                    Origin = dto.Origin,
                    Spec = dto.Spec,
                    Unit = dto.Unit,
                    Price = dto.Price,
                    Effect = dto.Effect,
                    Usage = dto.Usage,
                    Remark = dto.Remark,
                    Status = CommonStatus.Enabled
                };

                _context.Herbs.Add(herb);
                await _context.SaveChangesAsync();

                _logger.LogInformation("创建药材成功: {HerbName} ({HerbId})", herb.Name, herb.Id);

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
                    return ServiceResult<HerbDto>.Failure("药材ID不能为空");

                // 数据验证
                var validationResult = ValidateUpdateDto(dto);
                if (!validationResult.IsSuccess)
                    return ServiceResult<HerbDto>.Failure(validationResult.ErrorMessage);

                var herb = await _repository.GetByIdAsync(id);
                if (herb == null)
                    return ServiceResult<HerbDto>.Failure("药材不存在");

                // 检查名称重复（排除自己）
                if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != herb.Name)
                {
                    var existingHerb = await _context.Herbs
                        .FirstOrDefaultAsync(h => h.Name == dto.Name && h.Id != id && h.Status != CommonStatus.Disabled);
                    
                    if (existingHerb != null)
                        return ServiceResult<HerbDto>.Failure($"药材名称 '{dto.Name}' 已存在");
                }

                // 更新字段
                herb.Name = dto.Name;
                herb.PinYinCode = string.IsNullOrWhiteSpace(dto.PinYinCode)
                    ? GenerateSimplePinyinCode(dto.Name)
                    : dto.PinYinCode;
                herb.Origin = dto.Origin;
                herb.Spec = dto.Spec;
                herb.Unit = dto.Unit;
                herb.Price = dto.Price;
                herb.Effect = dto.Effect;
                herb.Usage = dto.Usage;
                herb.Remark = dto.Remark;
                herb.Status = dto.Status;

                _context.Herbs.Update(herb);
                await _context.SaveChangesAsync();

                _logger.LogInformation("更新药材成功: {HerbName} ({HerbId})", herb.Name, herb.Id);

                var resultDto = _mapper.Map<HerbDto>(herb);
                return ServiceResult<HerbDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新药材失败: {HerbId}", id);
                return ServiceResult<HerbDto>.Failure($"更新药材失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 软删除药材
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("药材ID不能为空");

                var herb = await _repository.GetByIdAsync(id);
                if (herb == null)
                    return ServiceResult<bool>.Failure("药材不存在");

                // 软删除 - 设置状态为Disabled
                herb.Status = CommonStatus.Disabled;
                _context.Herbs.Update(herb);
                await _context.SaveChangesAsync();

                _logger.LogInformation("软删除药材成功: {HerbName} ({HerbId})", herb.Name, herb.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除药材失败: {HerbId}", id);
                return ServiceResult<bool>.Failure($"删除药材失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置药材状态
        /// </summary>
        public async Task<ServiceResult<bool>> SetStatusAsync(Guid id, bool isActive)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("药材ID不能为空");

                var herb = await _repository.GetByIdAsync(id);
                if (herb == null)
                    return ServiceResult<bool>.Failure("药材不存在");

                herb.Status = isActive ? CommonStatus.Enabled : CommonStatus.Disabled;
                _context.Herbs.Update(herb);
                await _context.SaveChangesAsync();

                var statusText = isActive ? "启用" : "禁用";
                _logger.LogInformation("{Status}药材成功: {HerbName} ({HerbId})", statusText, herb.Name, herb.Id);
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置药材状态失败: {HerbId}", id);
                return ServiceResult<bool>.Failure($"设置药材状态失败: {ex.Message}");
            }
        }

        #region 私有方法

        /// <summary>
        /// 验证创建DTO
        /// </summary>
        private ServiceResult<bool> ValidateCreateDto(HerbCreateDto dto)
        {
            if (dto == null)
                return ServiceResult<bool>.Failure("药材信息不能为空");

            if (string.IsNullOrWhiteSpace(dto.Name))
                return ServiceResult<bool>.Failure("药材名称不能为空");

            if (dto.Name.Length > 50)
                return ServiceResult<bool>.Failure("药材名称不能超过50个字符");

            if (dto.Price <= 0)
                return ServiceResult<bool>.Failure("药材价格必须大于0");

            if (dto.Price > 9999.99m)
                return ServiceResult<bool>.Failure("药材价格不能超过9999.99");

            if (!string.IsNullOrWhiteSpace(dto.Unit) && dto.Unit.Length > 10)
                return ServiceResult<bool>.Failure("药材单位不能超过10个字符");

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证更新DTO
        /// </summary>
        private ServiceResult<bool> ValidateUpdateDto(HerbUpdateDto dto)
        {
            if (dto == null)
                return ServiceResult<bool>.Failure("药材信息不能为空");

            if (string.IsNullOrWhiteSpace(dto.Name))
                return ServiceResult<bool>.Failure("药材名称不能为空");

            if (dto.Name.Length > 50)
                return ServiceResult<bool>.Failure("药材名称不能超过50个字符");

            if (dto.Price <= 0)
                return ServiceResult<bool>.Failure("药材价格必须大于0");

            if (dto.Price > 9999.99m)
                return ServiceResult<bool>.Failure("药材价格不能超过9999.99");

            if (!string.IsNullOrWhiteSpace(dto.Unit) && dto.Unit.Length > 10)
                return ServiceResult<bool>.Failure("药材单位不能超过10个字符");

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 生成简单拼音码 - 基础实现
        /// </summary>
        private string GenerateSimplePinyinCode(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";

            // 简单拼音码生成：取每个字的首字母
            // 实际项目中可能需要更复杂的拼音转换库
            var result = "";
            foreach (char c in name)
            {
                if (char.IsLetter(c))
                {
                    result += char.ToUpper(c);
                }
                else if (c >= 0x4e00 && c <= 0x9fff) // 中文字符范围
                {
                    // 简化处理：中文字符用X代替
                    // 实际应该用拼音库转换
                    result += "X";
                }
            }

            return result.Length > 10 ? result.Substring(0, 10) : result;
        }

        #endregion
    }
}