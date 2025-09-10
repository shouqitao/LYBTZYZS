# 接口清理模板 - UltraThink双层架构标准

## 清理目标

解决接口重复定义和过度开发问题，统一实现UltraThink双层架构接口标准。

## 标准接口结构模板

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.[ModuleName];

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// [模块]服务接口 - UltraThink双层架构精简标准（小诊所适用）
    /// </summary>
    public interface I[Module]Service
    {
        #region 查询操作 - QueryService专业负责

        /// <summary>
        /// 分页查询[实体]
        /// </summary>
        Task<ServiceResult<PagedResult<[Module]Dto>>> GetPagedAsync([Module]PagedQueryDto query);
        
        /// <summary>
        /// 根据ID获取[实体]详情
        /// </summary>
        Task<ServiceResult<[Module]Dto>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// 搜索[实体]
        /// </summary>
        Task<ServiceResult<List<[Module]Dto>>> SearchAsync(string keyword);
        
        // 根据模块特性添加其他必需查询方法

        #endregion

        #region 业务操作 - BusinessService专业负责

        /// <summary>
        /// 创建新[实体]
        /// </summary>
        Task<ServiceResult<[Module]Dto>> CreateAsync([Module]CreateDto dto);
        
        /// <summary>
        /// 更新[实体]信息
        /// </summary>
        Task<ServiceResult<[Module]Dto>> UpdateAsync(Guid id, [Module]UpdateDto dto);
        
        /// <summary>
        /// 删除[实体]（软删除）
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);
        
        /// <summary>
        /// 启用[实体]
        /// </summary>
        Task<ServiceResult> EnableAsync(Guid id);
        
        /// <summary>
        /// 禁用[实体]
        /// </summary>
        Task<ServiceResult> DisableAsync(Guid id);

        #endregion

        #region 批量操作 - 必需功能（用户明确需求）

        /// <summary>
        /// 批量导入[实体]
        /// </summary>
        Task<ServiceResult<object>> Import[Module]sAsync(List<[Module]CreateDto> items);
        
        /// <summary>
        /// 导出[实体]数据
        /// </summary>
        Task<ServiceResult<byte[]>> Export[Module]sAsync(PagedQueryBaseDto query);

        #endregion
    }
}
```

## 清理规则

### ✅ 保留的功能

1. **核心查询**：
   - 分页查询 (`GetPagedAsync`)
   - 按ID查询 (`GetByIdAsync`) 
   - 关键字搜索 (`SearchAsync`)

2. **标准CRUD**：
   - 创建 (`CreateAsync`)
   - 更新 (`UpdateAsync`)  
   - 删除 (`DeleteAsync`)

3. **状态管理**：
   - 启用 (`EnableAsync`)
   - 禁用 (`DisableAsync`)

4. **批量操作** (用户明确要求)：
   - 患者、药材、验方需要批量导入导出功能

### ❌ 移除的功能

1. **重复方法**：
   - 同一概念的多个方法名 (如 `GetAllAsync` vs `GetListAsync`)
   - 功能重叠的查询方法

2. **过度开发功能**：
   - 复杂统计查询 (`GetStatisticsAsync`, `GetBasicStatisticsAsync`)
   - 高级分析功能 (`AnalyzeXxxAsync`)
   - 复杂权限检查
   - 审计日志功能

3. **不必要抽象**：
   - 过度业务流程包装 (`ProcessXxxAsync`)
   - 复杂推荐系统
   - 分享和协作功能

4. **小诊所不需要的功能**：
   - 库存过期管理
   - 复杂状态流转
   - 企业级审批流程

## 方法命名标准

### 查询方法
- `GetPagedAsync` - 分页查询
- `GetByIdAsync` - 按ID查询  
- `SearchAsync` - 关键字搜索
- `GetByXxxAsync` - 按特定条件查询 (如身份证号、电话等)

### CRUD方法
- `CreateAsync` - 创建
- `UpdateAsync` - 更新
- `DeleteAsync` - 删除
- `EnableAsync` / `DisableAsync` - 状态切换

### 批量方法
- `ImportXxxAsync` - 批量导入
- `ExportXxxAsync` - 批量导出

## 清理后的成果

### 用户模块 ✅
- **前**: 19个方法，功能重复混乱
- **后**: 12个方法，职责清晰分离

### 患者模块 ✅  
- **前**: 25个方法，状态管理冗余
- **后**: 12个方法，保留批量导入导出

### 验方模块 ✅
- **前**: 20个方法，复杂分析推荐功能
- **后**: 14个方法，简化为核心功能

### 药材模块 ✅
- **前**: 22个方法，复杂库存管理
- **后**: 12个方法，专注处方用药管理

## 实施效果

1. **代码精简**: 平均减少40%+的接口方法
2. **职责清晰**: 查询/业务/批量操作明确分离  
3. **维护性提升**: 统一命名规范，减少开发混乱
4. **符合需求**: 专注小诊所实际业务需求
5. **架构统一**: 所有模块遵循UltraThink双层架构标准

## 注意事项

1. **保留批量操作**: 患者、药材、验方必须有导入导出功能
2. **向后兼容**: 清理过程中确保不破坏现有功能调用
3. **逐步实施**: 完成接口清理后，需要更新对应的实现类
4. **测试验证**: 清理后需要验证编译和基本功能正常

## 下一步工作

1. 更新各模块的Desktop层接口以匹配Shared层
2. 更新Module、QueryService、BusinessService实现类
3. 修复编译错误
4. 验证系统基本功能正常