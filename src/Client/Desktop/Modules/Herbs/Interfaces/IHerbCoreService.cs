using LYBT.Shared.Models;
using LYBT.Shared.Models.Herbs;

namespace LYBT.Desktop.Herbs.Interfaces;

/// <summary>
/// 中药材核心服务接口 - UltraThink三层架构核心操作层
/// 职责：API通信、基础CRUD操作、数据验证
/// </summary>
public interface IHerbCoreService
{
    #region API通信操作
    
    /// <summary>
    /// 调用创建中药材API
    /// </summary>
    Task<ServiceResult<HerbDto>> CallCreateHerbApiAsync(HerbCreateDto createDto);
    
    /// <summary>
    /// 调用更新中药材API
    /// </summary>
    Task<ServiceResult<HerbDto>> CallUpdateHerbApiAsync(Guid id, HerbUpdateDto updateDto);
    
    /// <summary>
    /// 调用删除中药材API
    /// </summary>
    Task<ServiceResult<bool>> CallDeleteHerbApiAsync(Guid id);
    
    /// <summary>
    /// 调用获取中药材详情API
    /// </summary>
    Task<ServiceResult<HerbDto>> CallGetHerbByIdApiAsync(Guid id);
    
    /// <summary>
    /// 调用获取中药材列表API
    /// </summary>
    Task<ServiceResult<List<HerbDto>>> CallGetAllHerbsApiAsync();
    
    #endregion
    
    #region 基础数据操作
    
    /// <summary>
    /// 获取中药材基础信息
    /// </summary>
    Task<ServiceResult<HerbDto>> GetHerbByIdAsync(Guid id);
    
    /// <summary>
    /// 获取所有中药材列表
    /// </summary>
    Task<ServiceResult<List<HerbDto>>> GetAllHerbsAsync();
    
    /// <summary>
    /// 验证中药材ID是否存在
    /// </summary>
    Task<ServiceResult<bool>> ValidateHerbExistsAsync(Guid id);
    
    /// <summary>
    /// 获取中药材名称是否已存在
    /// </summary>
    Task<ServiceResult<bool>> CheckHerbNameExistsAsync(string name, Guid? excludeId = null);
    
    #endregion
    
    #region 数据验证操作
    
    /// <summary>
    /// 验证中药材创建数据
    /// </summary>
    ServiceResult ValidateHerbCreateData(HerbCreateDto createDto);
    
    /// <summary>
    /// 验证中药材更新数据
    /// </summary>
    ServiceResult ValidateHerbUpdateData(HerbUpdateDto updateDto);
    
    /// <summary>
    /// 验证价格数据有效性
    /// </summary>
    ServiceResult ValidatePriceData(decimal price);
    
    /// <summary>
    /// 验证药材基础信息
    /// </summary>
    ServiceResult ValidateHerbBasicInfo(string name, string category, string properties);
    
    #endregion
    
    #region 缓存和性能优化
    
    /// <summary>
    /// 预加载常用中药材到缓存
    /// </summary>
    Task<ServiceResult> PreloadCommonHerbsAsync();
    
    /// <summary>
    /// 清除中药材缓存
    /// </summary>
    ServiceResult ClearHerbCache();
    
    /// <summary>
    /// 获取缓存的中药材数据
    /// </summary>
    ServiceResult<List<HerbDto>> GetCachedHerbs();
    
    #endregion
}