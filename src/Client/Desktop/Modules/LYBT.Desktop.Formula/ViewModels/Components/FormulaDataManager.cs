using System.Collections.ObjectModel;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.ViewModels.Components
{
    /// <summary>
    /// 配方数据管理器 - 组件化架构实现
    /// Issue #1153: 负责配方数据的加载、刷新和状态管理
    /// </summary>
    public class FormulaDataManager
    {
        private readonly IFormulaRepository _repository;
        private readonly ILogger _logger;

        public FormulaDataManager(IFormulaRepository repository, ILogger logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 数据加载

        /// <summary>
        /// 加载配方详情
        /// </summary>
        public async Task<(bool success, FormulaDto? formula, string? errorMessage)> LoadFormulaAsync(Guid formulaId)
        {
            if (formulaId == Guid.Empty)
            {
                return (false, null, "配方ID无效");
            }

            try
            {
                _logger.LogInformation("加载配方详情: {FormulaId}", formulaId);

                var formula = await _repository.GetByIdAsync(formulaId);
                
                if (formula == null)
                {
                    return (false, null, "未找到指定的配方");
                }

                return (true, formula, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载配方详情时发生异常: {FormulaId}", formulaId);
                return (false, null, "加载配方详情时发生系统错误，请稍后重试");
            }
        }

        /// <summary>
        /// 刷新配方数据
        /// </summary>
        public async Task<(bool success, FormulaDto? formula, string? errorMessage)> RefreshFormulaAsync(Guid formulaId)
        {
            try
            {
                _logger.LogInformation("刷新配方数据: {FormulaId}", formulaId);

                return await LoadFormulaAsync(formulaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新配方数据时发生异常: {FormulaId}", formulaId);
                return (false, null, "刷新配方数据时发生系统错误");
            }
        }

        #endregion

        #region 药材集合管理

        /// <summary>
        /// 加载药材列表到集合
        /// </summary>
        public void LoadHerbItems(
            ObservableCollection<FormulaHerbItemDto> targetCollection,
            IEnumerable<FormulaHerbItemDto>? sourceItems)
        {
            if (targetCollection == null)
            {
                throw new ArgumentNullException(nameof(targetCollection));
            }

            try
            {
                targetCollection.Clear();

                if (sourceItems != null)
                {
                    foreach (var item in sourceItems)
                    {
                        targetCollection.Add(item);
                    }
                }

                _logger.LogDebug("已加载 {Count} 个药材项", targetCollection.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载药材列表时发生异常");
                throw;
            }
        }

        /// <summary>
        /// 添加药材项
        /// </summary>
        public void AddHerbItem(
            ObservableCollection<FormulaHerbItemDto> collection,
            FormulaHerbItemDto item)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            try
            {
                // 设置排序顺序
                item.SortOrder = collection.Count;
                collection.Add(item);

                _logger.LogDebug("添加药材项: {HerbName}", item.HerbName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加药材项时发生异常: {HerbName}", item.HerbName);
                throw;
            }
        }

        /// <summary>
        /// 移除药材项
        /// </summary>
        public bool RemoveHerbItem(
            ObservableCollection<FormulaHerbItemDto> collection,
            FormulaHerbItemDto item)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            try
            {
                var result = collection.Remove(item);
                
                if (result)
                {
                    // 重新排序
                    ReorderHerbItems(collection);
                    _logger.LogDebug("移除药材项: {HerbName}", item.HerbName);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除药材项时发生异常: {HerbName}", item.HerbName);
                throw;
            }
        }

        /// <summary>
        /// 清空药材列表
        /// </summary>
        public void ClearHerbItems(ObservableCollection<FormulaHerbItemDto> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            try
            {
                var count = collection.Count;
                collection.Clear();
                _logger.LogDebug("已清空 {Count} 个药材项", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空药材列表时发生异常");
                throw;
            }
        }

        /// <summary>
        /// 重新排序药材项
        /// </summary>
        public void ReorderHerbItems(ObservableCollection<FormulaHerbItemDto> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            try
            {
                for (int i = 0; i < collection.Count; i++)
                {
                    collection[i].SortOrder = i;
                }

                _logger.LogDebug("重新排序 {Count} 个药材项", collection.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新排序药材项时发生异常");
                throw;
            }
        }

        /// <summary>
        /// 移动药材项位置
        /// </summary>
        public bool MoveHerbItem(
            ObservableCollection<FormulaHerbItemDto> collection,
            int oldIndex,
            int newIndex)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (oldIndex < 0 || oldIndex >= collection.Count ||
                newIndex < 0 || newIndex >= collection.Count)
            {
                return false;
            }

            try
            {
                collection.Move(oldIndex, newIndex);
                ReorderHerbItems(collection);

                _logger.LogDebug("移动药材项: {OldIndex} → {NewIndex}", oldIndex, newIndex);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移动药材项时发生异常: {OldIndex} → {NewIndex}", oldIndex, newIndex);
                throw;
            }
        }

        #endregion

        #region 数据状态管理

        /// <summary>
        /// 检查配方是否已加载
        /// </summary>
        public bool IsFormulaLoaded(FormulaDto? formula)
        {
            return formula != null && formula.Id != Guid.Empty;
        }

        /// <summary>
        /// 检查配方是否有药材
        /// </summary>
        public bool HasHerbItems(FormulaDto? formula)
        {
            return formula?.Herbs != null && formula.Herbs.Any();
        }

        /// <summary>
        /// 获取药材数量
        /// </summary>
        public int GetHerbItemCount(IEnumerable<FormulaHerbItemDto>? items)
        {
            return items?.Count() ?? 0;
        }

        /// <summary>
        /// 获取配方总价格
        /// </summary>
        public decimal CalculateTotalPrice(IEnumerable<FormulaHerbItemDto>? items)
        {
            if (items == null || !items.Any())
            {
                return 0m;
            }

            try
            {
                return items.Sum(h => h.Price * h.Quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算配方总价格时发生异常");
                return 0m;
            }
        }

        /// <summary>
        /// 创建配方数据快照（用于取消编辑时恢复）
        /// </summary>
        public FormulaDataSnapshot CreateSnapshot(FormulaDto formula)
        {
            if (formula == null)
            {
                throw new ArgumentNullException(nameof(formula));
            }

            return new FormulaDataSnapshot
            {
                Name = formula.Name,
                Effect = formula.Effect,
                Usage = formula.Usage,
                Property = formula.Property,
                Remark = formula.Remark,
                IsShared = formula.IsShared,
                Category = formula.Category,
                HerbItems = formula.Herbs?.Select(h => new FormulaHerbItemDto
                {
                    Id = h.Id,
                    HerbId = h.HerbId,
                    HerbName = h.HerbName,
                    Quantity = h.Quantity,
                    Preparation = h.Preparation,
                    Usage = h.Usage,
                    SortOrder = h.SortOrder,
                    Price = h.Price
                }).ToList() ?? new List<FormulaHerbItemDto>()
            };
        }

        /// <summary>
        /// 从快照恢复数据
        /// </summary>
        public void RestoreFromSnapshot(
            FormulaDataSnapshot snapshot,
            Action<string> setName,
            Action<string> setEffect,
            Action<string> setUsage,
            Action<string> setProperty,
            Action<string> setRemark,
            Action<bool> setIsShared,
            Action<string> setCategory,
            ObservableCollection<FormulaHerbItemDto> herbCollection)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            try
            {
                setName?.Invoke(snapshot.Name ?? string.Empty);
                setEffect?.Invoke(snapshot.Effect ?? string.Empty);
                setUsage?.Invoke(snapshot.Usage ?? string.Empty);
                setProperty?.Invoke(snapshot.Property ?? string.Empty);
                setRemark?.Invoke(snapshot.Remark ?? string.Empty);
                setIsShared?.Invoke(snapshot.IsShared);
                setCategory?.Invoke(snapshot.Category ?? string.Empty);

                LoadHerbItems(herbCollection, snapshot.HerbItems);

                _logger.LogDebug("已从快照恢复数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从快照恢复数据时发生异常");
                throw;
            }
        }

        #endregion
    }

    /// <summary>
    /// 配方数据快照 - 用于编辑取消时恢复原始数据
    /// </summary>
    public class FormulaDataSnapshot
    {
        public string? Name { get; set; }
        public string? Effect { get; set; }
        public string? Usage { get; set; }
        public string? Property { get; set; }
        public string? Remark { get; set; }
        public bool IsShared { get; set; }
        public string? Category { get; set; }
        public List<FormulaHerbItemDto> HerbItems { get; set; } = new();
    }
}
