using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.Components
{
    /// <summary>
    /// 药材数据管理器
    /// Epic #1773: Herbs模块组件化改造
    ///
    /// 职责:
    /// - 管理药材实体数据
    /// - 保存药材信息（CreateAsync/UpdateAsync）
    /// - 变更检测
    /// </summary>
    public class HerbDataManager : IDataManager<HerbDto>
    {
        #region 字段

        private readonly IHerbRepository _herbRepository;
        private readonly ILogger<HerbDataManager> _logger;

        // 药材数据
        private HerbDto? _originalHerb;
        private HerbDto? _currentHerb;

        #endregion

        #region 属性

        /// <summary>
        /// 当前药材数据
        /// </summary>
        public virtual HerbDto? Current => _currentHerb;

        /// <summary>
        /// 是否有未保存的更改
        /// </summary>
        public virtual bool HasChanges
        {
            get
            {
                if (_currentHerb == null || _originalHerb == null)
                    return false;

                return IsHerbChanged();
            }
        }

        #endregion

        #region 构造函数

        public HerbDataManager(
            IHerbRepository herbRepository,
            ILogger<HerbDataManager> logger)
        {
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region IDataManager实现

        /// <summary>
        /// 初始化药材数据（通过HerbId加载）
        /// </summary>
        /// <param name="entityId">药材ID</param>
        public async Task InitializeAsync(Guid entityId)
        {
            try
            {
                _logger.LogInformation("开始加载药材数据: HerbId={HerbId}", entityId);

                var herb = await _herbRepository.GetByIdAsync(entityId);

                if (herb != null)
                {
                    _currentHerb = herb;
                    _originalHerb = CloneHerb(herb);

                    _logger.LogInformation("药材数据加载成功: HerbId={HerbId}", herb.Id);
                }
                else
                {
                    _logger.LogWarning("未找到药材数据: HerbId={HerbId}", entityId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载药材数据失败: HerbId={HerbId}", entityId);
                throw;
            }
        }

        /// <summary>
        /// 保存药材数据
        /// </summary>
        public virtual async Task<bool> SaveAsync()
        {
            if (_currentHerb == null)
            {
                _logger.LogWarning("无法保存：当前药材数据为空");
                return false;
            }

            try
            {
                _logger.LogInformation("开始保存药材数据: HerbName={HerbName}", _currentHerb.Name);

                HerbDto? result;
                if (_currentHerb.Id == Guid.Empty)
                {
                    // 创建新药材
                    var inputDto = ToInputDto(_currentHerb);
                    result = await _herbRepository.CreateAsync(inputDto);
                }
                else
                {
                    // 更新现有药材
                    var inputDto = ToInputDto(_currentHerb);
                    result = await _herbRepository.UpdateAsync(inputDto);
                }

                if (result != null)
                {
                    _currentHerb = result;
                    _originalHerb = CloneHerb(result);

                    _logger.LogInformation("药材数据保存成功");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存药材数据失败: HerbName={HerbName}", _currentHerb?.Name);
                return false;
            }
        }

        /// <summary>
        /// 删除药材数据
        /// </summary>
        public virtual async Task<bool> DeleteAsync()
        {
            if (_currentHerb == null || _currentHerb.Id == Guid.Empty)
            {
                _logger.LogWarning("无法删除：当前药材数据为空或ID无效");
                return false;
            }

            try
            {
                _logger.LogInformation("开始删除药材: HerbId={HerbId}", _currentHerb.Id);

                await _herbRepository.DeleteAsync(_currentHerb.Id);

                _currentHerb = null;
                _originalHerb = null;

                _logger.LogInformation("药材删除成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除药材失败: HerbId={HerbId}", _currentHerb?.Id);
                return false;
            }
        }

        /// <summary>
        /// 重新加载药材数据
        /// </summary>
        public virtual async Task ReloadAsync()
        {
            if (_currentHerb != null && _currentHerb.Id != Guid.Empty)
            {
                _logger.LogInformation("重新加载药材数据: HerbId={HerbId}", _currentHerb.Id);
                await InitializeAsync(_currentHerb.Id);
            }
        }

        #endregion

        #region 数据操作方法

        /// <summary>
        /// 更新药材数据
        /// </summary>
        public void UpdateHerb(HerbDto herb)
        {
            if (herb == null)
                throw new ArgumentNullException(nameof(herb));

            _currentHerb = herb;
        }

        /// <summary>
        /// 创建新药材（设置为当前数据）
        /// </summary>
        public void CreateNew()
        {
            _currentHerb = new HerbDto
            {
                Id = Guid.Empty,
                Name = string.Empty,
                Unit = "克",
                Price = 0m,
                Status = Shared.Models.Enums.CommonStatus.Enabled
            };
            _originalHerb = null;
        }

        /// <summary>
        /// 通过ID获取药材（Repository方法）
        /// Epic #1773: 为ViewModel提供获取药材功能
        /// </summary>
        public virtual async Task<HerbDto?> GetByIdAsync(Guid herbId)
        {
            try
            {
                _logger.LogDebug("获取药材: HerbId={HerbId}", herbId);
                var herb = await _herbRepository.GetByIdAsync(herbId);
                _logger.LogInformation("药材获取成功: HerbId={HerbId}", herbId);
                return herb;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材失败: HerbId={HerbId}", herbId);
                throw;
            }
        }

        /// <summary>
        /// 创建新药材（Repository方法）
        /// Epic #1773: 为ViewModel提供创建药材功能
        /// </summary>
        public virtual async Task<HerbDto?> CreateAsync(HerbInputDto inputDto)
        {
            try
            {
                _logger.LogDebug("创建药材: HerbName={HerbName}", inputDto.Name);
                var herb = await _herbRepository.CreateAsync(inputDto);
                _logger.LogInformation("药材创建成功: HerbName={HerbName}", herb?.Name);
                return herb;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建药材失败: HerbName={HerbName}", inputDto.Name);
                throw;
            }
        }

        /// <summary>
        /// 更新药材（Repository方法）
        /// Epic #1773: 为ViewModel提供更新药材功能
        /// </summary>
        public virtual async Task<HerbDto?> UpdateAsync(HerbInputDto inputDto)
        {
            try
            {
                _logger.LogDebug("更新药材: HerbName={HerbName}", inputDto.Name);
                var herb = await _herbRepository.UpdateAsync(inputDto);
                _logger.LogInformation("药材更新成功: HerbName={HerbName}", herb?.Name);
                return herb;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新药材失败: HerbName={HerbName}", inputDto.Name);
                throw;
            }
        }

        #endregion

        #region 私有方法 - 变更检测

        private bool IsHerbChanged()
        {
            if (_currentHerb == null || _originalHerb == null)
                return false;

            return _currentHerb.Name != _originalHerb.Name ||
                   _currentHerb.PinYinCode != _originalHerb.PinYinCode ||
                   _currentHerb.Origin != _originalHerb.Origin ||
                   _currentHerb.Spec != _originalHerb.Spec ||
                   _currentHerb.Unit != _originalHerb.Unit ||
                   _currentHerb.Price != _originalHerb.Price ||
                   _currentHerb.CostPrice != _originalHerb.CostPrice ||
                   _currentHerb.Effect != _originalHerb.Effect ||
                   _currentHerb.Usage != _originalHerb.Usage ||
                   _currentHerb.Remark != _originalHerb.Remark ||
                   _currentHerb.Status != _originalHerb.Status;
        }

        #endregion

        #region 私有方法 - 深拷贝

        private HerbDto CloneHerb(HerbDto source)
        {
            return new HerbDto
            {
                Id = source.Id,
                Name = source.Name,
                PinYinCode = source.PinYinCode,
                Origin = source.Origin,
                Spec = source.Spec,
                Unit = source.Unit,
                Price = source.Price,
                CostPrice = source.CostPrice,
                Effect = source.Effect,
                Usage = source.Usage,
                Remark = source.Remark,
                Status = source.Status,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };
        }

        private HerbInputDto ToInputDto(HerbDto dto)
        {
            return new HerbInputDto
            {
                Id = dto.Id == Guid.Empty ? null : dto.Id,
                Name = dto.Name,
                PinYinCode = dto.PinYinCode,
                Origin = dto.Origin,
                Spec = dto.Spec,
                Unit = dto.Unit,
                Price = dto.Price,
                CostPrice = dto.CostPrice,
                Effect = dto.Effect,
                Usage = dto.Usage,
                Status = dto.Status
            };
        }

        #endregion
    }
}
