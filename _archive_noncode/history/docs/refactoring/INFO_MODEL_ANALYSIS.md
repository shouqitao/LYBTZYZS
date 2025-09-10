# Info模型使用情况分析报告

> 分析日期：2025-01-17  
> 目标：评估移除Info层的影响范围和复杂度

## 📊 Info模型统计

### 核心业务Info模型（需要移除）
| Info模型 | 文件位置 | 使用文件数 | 复杂度 | 优先级 |
|---------|----------|-----------|-------|--------|
| UserInfo | Core/Models/Users/ | 25+ | 高 | P0 |
| PatientInfo | Core/Models/Patients/ | 30+ | 高 | P0 |
| HerbInfo | Core/Models/Herbs/ | 15+ | 中 | P1 |
| ConsultationInfo | Core/Models/Consultation/ | 20+ | 高 | P1 |
| PrescriptionInfo | Core/Models/Prescriptions/ | 18+ | 中 | P1 |
| FormulaInfo | Core/Models/Formulas/ | 12+ | 中 | P2 |
| MedicalCaseInfo | Core/Models/MedicalCase/ | 10+ | 中 | P2 |

### 辅助Info模型（特殊处理）
| Info模型 | 用途 | 处理策略 |
|---------|------|---------|
| LoginInfo | 认证状态管理 | 保留，特殊处理 |
| AuthSessionInfo | 会话管理 | 保留，特殊处理 |
| SecurityLogInfo | 安全日志 | 保留，基础设施 |
| BackupInfo | 备份管理 | 保留，系统功能 |
| SettingInfo | 配置管理 | 保留，系统功能 |

## 🔍 主要使用场景分析

### 1. UserInfo使用模式
**高频使用区域**:
- ViewModels: 25个ViewModel直接使用
- Services: 8个ModuleService返回UserInfo
- Mapping: 双向映射配置(DTO↔Info)
- Extensions: 专门的转换扩展方法

**复杂依赖**:
```csharp
// 典型的复杂使用模式
public async Task<ServiceResult<PagedResult<UserInfo>>> GetPagedAsync(PagedQueryBaseDto query)
{
    var apiResult = await _userApi.GetUsersAsync(query);
    var userInfos = _mapper.Map<List<UserInfo>>(apiResult.Data.Items);
    return ServiceResult<PagedResult<UserInfo>>.Success(result);
}
```

### 2. PatientInfo使用模式
**跨模块依赖**:
- Consultation模块: 患者选择和数据协调
- MedicalCase模块: 病例创建时患者信息
- Prescriptions模块: 开方时患者信息

**事件系统集成**:
```csharp
public class PatientSelectedEvent : PubSubEvent<PatientInfo>
public void PublishPatientSelected(PatientInfo patient)
```

### 3. 其他Info模型
- **HerbInfo**: 主要在药材管理和处方模块使用
- **ConsultationInfo**: 看诊流程的核心数据模型
- **PrescriptionInfo**: 处方管理的主要数据载体

## ⚠️ 重构风险点

### 高风险区域
1. **AutoMapper配置**: 需要完全重写映射规则
2. **事件系统**: PatientSelectedEvent等需要重构
3. **ViewModel绑定**: 所有集合和属性绑定需要更新
4. **扩展方法**: DtoToInfoExtensions.cs需要删除

### 中等风险区域
1. **服务接口**: ModuleService返回类型需要修改
2. **验证逻辑**: Info模型中的验证需要迁移到DTO
3. **缓存逻辑**: 基于Info的缓存需要调整

### 低风险区域
1. **UI显示**: XAML绑定相对容易修改
2. **基础设施**: 系统级Info模型可以保留

## 🎯 迁移策略建议

### 阶段1: 核心模型迁移（UserInfo, PatientInfo）
- 影响最大，优先处理
- 需要完整的DTO扩展和UI适配
- 预计工作量：2天

### 阶段2: 业务模型迁移（Herb, Consultation, Prescription）
- 中等复杂度，逐个处理
- 可以并行进行
- 预计工作量：1.5天

### 阶段3: 辅助模型迁移（Formula, MedicalCase）
- 相对简单，最后处理
- 依赖前面的基础工作
- 预计工作量：0.5天

## 📋 DTO扩展需求识别

基于Info模型分析，以下DTO需要扩展UI辅助属性：

### UserDto扩展需求
```csharp
// 需要从UserInfo迁移的UI属性
public string DisplayName { get; set; }        // 显示名称组合
public string StatusText { get; set; }         // 状态文本
public string RoleDisplayName { get; set; }    // 角色显示名
public bool CanEdit { get; set; }              // 编辑权限
public bool CanDelete { get; set; }            // 删除权限
public string AvatarUrl { get; set; }          // 头像URL
```

### PatientDto扩展需求
```csharp
// 需要从PatientInfo迁移的UI属性  
public string DisplayName { get; set; }        // 患者显示名
public string AgeDisplay { get; set; }         // 年龄显示
public string GenderDisplay { get; set; }      // 性别显示
public string StatusText { get; set; }         // 状态文本
public bool CanEdit { get; set; }              // 编辑权限
public string LastVisitDisplay { get; set; }   // 最后就诊显示
```

### 其他DTO扩展
- HerbDto: 库存状态、价格显示、可用性标识
- ConsultationDto: 状态显示、进度信息、时长计算
- PrescriptionDto: 患者信息显示、状态文本、总价计算
- FormulaDto: 适用症显示、药材数量、成本计算
- MedicalCaseDto: 状态显示、关联信息、时间计算

## 📊 工作量估算

| 任务类型 | 文件数量 | 预估工时 | 风险级别 |
|---------|----------|---------|---------|
| 删除Info模型文件 | 33个 | 0.5天 | 低 |
| 更新ViewModel | 45个 | 1.5天 | 中 |
| 修改服务层 | 8个 | 1天 | 高 |
| 重写映射配置 | 1个 | 0.5天 | 高 |
| 更新XAML绑定 | 30个 | 1天 | 中 |
| 扩展DTO模型 | 7个 | 0.5天 | 中 |
| **总计** | **124个** | **5天** | **中-高** |

## ✅ 成功标准

1. **编译通过**: 0错误，0警告
2. **功能完整**: 所有UI功能正常工作
3. **性能提升**: 内存使用减少30%+
4. **代码简化**: 映射配置减少50%
5. **可维护性**: 新增字段影响文件数减少50%

这个分析为后续的重构工作提供了详细的路线图和风险控制策略。